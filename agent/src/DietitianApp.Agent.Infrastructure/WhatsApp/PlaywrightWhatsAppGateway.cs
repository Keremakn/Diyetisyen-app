using System.Text;
using DietitianApp.Agent.Application.Abstractions;
using DietitianApp.Agent.Application.Models;
using DietitianApp.Agent.Infrastructure.Configuration;
using DietitianApp.Agent.Infrastructure.Paths;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace DietitianApp.Agent.Infrastructure.WhatsApp;

public sealed class PlaywrightWhatsAppGateway(
    IAppPathProvider paths,
    IOptions<WhatsAppOptions> options,
    IOptions<WhatsAppSelectors> selectors,
    ILogger<PlaywrightWhatsAppGateway> log) : IWhatsAppGateway, IArtifactService, IAsyncDisposable
{
    private IPlaywright? playwright;
    private IBrowserContext? context;
    private IPage? page;
    private bool trace;

    public async Task<WhatsAppSessionResult> EnsureSessionAsync(CancellationToken token)
    {
        try
        {
            var p = await GetPageAsync(token);
            if (await AnyAsync(p, selectors.Value.Captcha))
                return new(false, "CAPTCHA", "Guvenlik dogrulamasi algilandi.");
            if (await AnyAsync(p, selectors.Value.Ready))
                return new(true);

            var end = DateTimeOffset.UtcNow.AddSeconds(options.Value.LoginTimeoutSeconds);
            while (DateTimeOffset.UtcNow < end)
            {
                token.ThrowIfCancellationRequested();
                if (await AnyAsync(p, selectors.Value.Captcha))
                    return new(false, "CAPTCHA", "Guvenlik dogrulamasi algilandi.");
                if (await AnyAsync(p, selectors.Value.Ready))
                    return new(true);
                await Task.Delay(1000, token);
            }

            return new(false, "SESSION_NOT_READY", "WhatsApp oturumu hazir degil.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogError(ex, "WhatsApp session failed");
            return new(false, "SESSION_FAILED", "WhatsApp acilamadi.");
        }
    }

    public async Task<GroupVerificationResult> VerifyGroupAsync(string name, CancellationToken token)
    {
        var session = await EnsureSessionAsync(token);
        if (!session.Success)
            return new(false, session.ErrorCode, session.ErrorMessage);

        var result = await OpenExactChatAsync(name, token);
        return new(result, null, result ? null : "Tam eslesen grup bulunamadi.");
    }

    public async Task<SendMessageResult> SendMessageAsync(string name, string message, CancellationToken token)
    {
        try
        {
            if (!await OpenExactChatAsync(name, token))
                return await Failure("GROUP_NOT_FOUND", "Tam eslesen grup bulunamadi.", token);

            var p = page!;
            var box = await VisibleAsync(p, selectors.Value.MessageBoxes, 3000);
            if (box is null)
                return await Failure("MESSAGE_BOX_NOT_FOUND", "Mesaj alani bulunamadi.", token);

            await box.ClickAsync();
            await p.Keyboard.PressAsync("Control+A");
            await p.Keyboard.PressAsync("Backspace");
            await box.FillAsync(message);

            if (!string.Equals((await box.InnerTextAsync()).Trim(), message, StringComparison.Ordinal))
                return await Failure("COMPOSER_MISMATCH", "Mesaj alani dogrulanamadi.", token);

            var literal = XPath(message);
            var bubbles = p.Locator($"xpath=//*[@id='main']//*[@data-testid='msg-container' and .//*[@data-testid='selectable-text' and normalize-space(.)={literal}]]");
            var before = await bubbles.CountAsync();
            var send = p.Locator("footer span[data-icon='send']").First;

            if (await send.IsVisibleAsync())
                await send.ClickAsync();
            else
                await p.Keyboard.PressAsync("Enter");

            var end = DateTimeOffset.UtcNow.AddSeconds(12);
            while (DateTimeOffset.UtcNow < end)
            {
                token.ThrowIfCancellationRequested();

                if (await bubbles.CountAsync() > before)
                {
                    var last = bubbles.Last;
                    if (await HasSendErrorAsync(last))
                        return await Failure("WHATSAPP_SEND_ERROR", "WhatsApp mesaji gonderemedi.", token);

                    if (await HasDeliveryTickAsync(last))
                        return new(true);

                    // WhatsApp sometimes paints the outgoing bubble before exposing tick metadata.
                    // Seeing the new outgoing bubble for a few seconds is enough for Phase 1 MVP.
                    if (DateTimeOffset.UtcNow > end.AddSeconds(-8))
                        return new(true);
                }

                await Task.Delay(250, token);
            }

            return await Failure("SEND_NOT_CONFIRMED", "Mesaj balonu goruldu ancak tik ile dogrulanamadi.", token);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Send failed for {Group}", name);
            return await Failure("AUTOMATION_FAILED", "WhatsApp otomasyonu hata verdi.", token);
        }
    }

    private async Task<bool> OpenExactChatAsync(string raw, CancellationToken token)
    {
        var name = raw.Normalize(NormalizationForm.FormC);
        var p = await GetPageAsync(token);
        var search = await VisibleAsync(p, selectors.Value.SearchBoxes, 3000);
        if (search is null)
            return false;

        await search.ClickAsync();
        await p.Keyboard.PressAsync("Control+A");
        await p.Keyboard.PressAsync("Backspace");
        await search.FillAsync(name);

        var exact = p.Locator($"xpath=//span[@title={XPath(name)}]").First;
        if (!await WaitAsync(exact, options.Value.ActionTimeoutSeconds * 1000))
            return false;

        await exact.ClickAsync();
        var header = p.Locator($"xpath=//header//*[normalize-space(.)={XPath(name)}]").Last;
        return await WaitAsync(header, options.Value.ActionTimeoutSeconds * 1000);
    }

    private async Task<IPage> GetPageAsync(CancellationToken token)
    {
        if (page is not null)
            return page;

        paths.EnsureDirectories();
        playwright = await Playwright.CreateAsync();
        context = await playwright.Chromium.LaunchPersistentContextAsync(
            paths.BrowserProfilePath,
            new()
            {
                Headless = options.Value.Headless,
                Timeout = options.Value.NavigationTimeoutSeconds * 1000,
                Args = ["--disable-blink-features=AutomationControlled"]
            });

        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });
        trace = true;
        page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();
        await page.GotoAsync(options.Value.Url, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        token.ThrowIfCancellationRequested();
        return page;
    }

    public async Task<string?> CaptureScreenshotAsync(CancellationToken token)
    {
        if (page is null)
            return null;

        paths.EnsureDirectories();
        var path = Path.Combine(paths.ScreenshotsPath, $"failure-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.png");
        await page.ScreenshotAsync(new() { Path = path, FullPage = true });
        return path;
    }

    public async Task<string?> SaveTraceAsync(CancellationToken token)
    {
        if (context is null || !trace)
            return null;

        var path = Path.Combine(paths.TracesPath, $"trace-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.zip");
        await context.Tracing.StopAsync(new() { Path = path });
        trace = false;
        return path;
    }

    private async Task<SendMessageResult> Failure(string code, string msg, CancellationToken token)
    {
        string? shot = null;
        string? tr = null;
        try
        {
            shot = await CaptureScreenshotAsync(token);
            tr = await SaveTraceAsync(token);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Artifact capture failed");
        }

        return new(false, code, msg, shot, tr);
    }

    private static async Task<ILocator?> VisibleAsync(IPage p, IEnumerable<string> ss, float timeout)
    {
        foreach (var s in ss)
        {
            var l = p.Locator(s).First;
            if (await WaitAsync(l, timeout))
                return l;
        }

        return null;
    }

    private static async Task<bool> WaitAsync(ILocator l, float timeout)
    {
        try
        {
            await l.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeout });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
        catch (PlaywrightException)
        {
            return false;
        }
    }

    private static async Task<bool> AnyAsync(IPage p, IEnumerable<string> ss) => await VisibleAsync(p, ss, 500) is not null;

    private static async Task<bool> HasDeliveryTickAsync(ILocator message)
    {
        var ticks = message.Locator("xpath=.//*[@data-icon='msg-check' or @data-icon='msg-dblcheck' or @data-icon='msg-dblcheck-ack'] | .//*[local-name()='title' and (contains(.,'check') or contains(.,'read') or contains(.,'sent') or contains(.,'delivered') or contains(.,'okundu') or contains(.,'teslim'))]");
        return await ticks.CountAsync() > 0;
    }

    private static async Task<bool> HasSendErrorAsync(ILocator message)
    {
        var errors = message.Locator("xpath=.//*[@data-icon='msg-error' or @data-icon='error' or contains(@aria-label,'failed') or contains(@title,'failed')]");
        return await errors.CountAsync() > 0;
    }

    private static string XPath(string v) =>
        !v.Contains('\'') ? $"'{v}'" :
        !v.Contains('"') ? $"\"{v}\"" :
        "concat(" + string.Join(", \"'\", ", v.Split('\'').Select(x => $"'{x}'")) + ")";

    public async ValueTask DisposeAsync()
    {
        if (context is not null)
            await context.CloseAsync();

        playwright?.Dispose();
    }
}
