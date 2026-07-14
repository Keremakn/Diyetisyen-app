using DietitianApp.Agent.Poc.Application.Abstractions;
using DietitianApp.Agent.Poc.Configuration;
using DietitianApp.Agent.Poc.Infrastructure.Artifacts;
using DietitianApp.Agent.Poc.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using System.Text;

namespace DietitianApp.Agent.Poc.Infrastructure.WhatsApp;

public sealed class WhatsAppAutomationService : IWhatsAppAutomationService
{
    private readonly WhatsAppBrowserSession _browserSession;
    private readonly ScreenshotService _screenshotService;
    private readonly AppOptions _options;
    private readonly WhatsAppSelectorsOptions _selectors;
    private readonly ILogger<WhatsAppAutomationService> _logger;

    public WhatsAppAutomationService(
        WhatsAppBrowserSession browserSession,
        ScreenshotService screenshotService,
        IOptions<AppOptions> options,
        IOptions<WhatsAppSelectorsOptions> selectors,
        ILogger<WhatsAppAutomationService> logger)
    {
        _browserSession = browserSession;
        _screenshotService = screenshotService;
        _options = options.Value;
        _selectors = selectors.Value;
        _logger = logger;
    }

    public async Task<AutomationResult> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var groupName = request.GroupName.Normalize(NormalizationForm.FormC);
            var page = await _browserSession.GetPageAsync(cancellationToken);

            var searchBox = await FirstVisibleAsync(page, _selectors.SearchBoxSelectors);
            if (searchBox is null)
            {
                var searchActivator = await FirstVisibleAsync(page, _selectors.SearchActivationSelectors);
                if (searchActivator is not null)
                {
                    await searchActivator.ClickAsync();
                    searchBox = await FirstVisibleAsync(page, _selectors.SearchBoxSelectors, 5_000);
                }
            }

            if (searchBox is null)
            {
                return await FailWithArtifactsAsync(
                    "Arama kutusu bulunamadi. Mesaj gonderilmedi.",
                    cancellationToken);
            }

            await searchBox.ClickAsync();
            await ClearFocusedInputAsync(page);
            await searchBox.FillAsync(groupName);
            await page.WaitForTimeoutAsync(1_000);

            var exactGroup = page.Locator($"xpath=//span[@title={ToXPathLiteral(groupName)}]").First;
            if (!await WaitUntilVisibleAsync(exactGroup, _options.ActionTimeoutSeconds * 1_000))
            {
                return AutomationResult.Fail($"Tam eslesen grup bulunamadi: {groupName}");
            }

            await exactGroup.ClickAsync();

            var exactHeaderTitle = page
                .Locator($"xpath=//header//*[normalize-space(.)={ToXPathLiteral(groupName)}]")
                .Last;

            if (!await WaitUntilVisibleAsync(exactHeaderTitle, _options.ActionTimeoutSeconds * 1_000))
            {
                return await FailWithArtifactsAsync(
                    $"Secilen sohbet basliginda tam grup adi dogrulanamadi. Beklenen: '{groupName}'.",
                    cancellationToken);
            }

            var messageBox = await FirstVisibleAsync(page, _selectors.MessageBoxSelectors);
            if (messageBox is null)
            {
                return AutomationResult.Fail("Mesaj kutusu bulunamadi. Mesaj gonderilmedi.");
            }

            await messageBox.ClickAsync();
            await messageBox.FillAsync(request.Message);

            var sentMessageTextOutsideComposer = page.Locator(
                $"xpath=//*[not(ancestor-or-self::footer) and " +
                $"normalize-space(.)={ToXPathLiteral(request.Message)}]");
            var matchingTextCountBeforeSend = await sentMessageTextOutsideComposer.CountAsync();

            var sendButton = await FirstVisibleAsync(
                page,
                [
                    "footer button[aria-label='Gönder']",
                    "footer button[aria-label='Send']",
                    "footer span[data-icon='send']"
                ],
                1_000);

            if (sendButton is not null)
            {
                await sendButton.ClickAsync();
            }
            else
            {
                await page.Keyboard.PressAsync("Enter");
            }

            if (!await WaitForCountIncreaseAsync(
                    sentMessageTextOutsideComposer,
                    matchingTextCountBeforeSend,
                    10_000,
                    cancellationToken))
            {
                return await FailWithArtifactsAsync(
                    "Mesaj gonderme islemi tetiklendi ancak giden mesaj balonunda dogrulanamadi.",
                    cancellationToken);
            }

            _logger.LogInformation("Message sent to exact test group {GroupName}.", groupName);
            return AutomationResult.Ok($"Mesaj gonderildi: {groupName}");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp automation failed.");
            var screenshotPath = await CaptureScreenshotSafeAsync(cancellationToken);
            var tracePath = await StopTraceSafeAsync();
            return AutomationResult.Fail("WhatsApp otomasyonu hata verdi. Artefact dosyalari kaydedildi.", screenshotPath, tracePath);
        }
    }

    private async Task<ILocator?> FirstVisibleAsync(
        IPage page,
        IReadOnlyCollection<string> selectors,
        float? probeTimeout = null)
    {
        var timeout = probeTimeout ?? Math.Min(_options.ActionTimeoutSeconds * 1_000, 1_500);
        foreach (var selector in selectors)
        {
            try
            {
                var locator = page.Locator(selector).First;
                if (await WaitUntilVisibleAsync(locator, timeout))
                {
                    return locator;
                }
            }
            catch (PlaywrightException ex)
            {
                _logger.LogDebug(ex, "Selector candidate failed: {Selector}", selector);
            }
        }

        return null;
    }

    private static async Task<bool> WaitUntilVisibleAsync(ILocator locator, float timeout)
    {
        try
        {
            await locator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = timeout
            });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    private static async Task ClearFocusedInputAsync(IPage page)
    {
        var modifier = OperatingSystem.IsMacOS() ? "Meta" : "Control";
        await page.Keyboard.PressAsync($"{modifier}+A");
        await page.Keyboard.PressAsync("Backspace");
    }

    private async Task<string?> CaptureScreenshotSafeAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _screenshotService.CaptureAsync(_browserSession.CurrentPage, _options.ScreenshotsDirectory, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to capture screenshot.");
            return null;
        }
    }

    private static async Task<bool> WaitForCountIncreaseAsync(
        ILocator locator,
        int initialCount,
        int timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.AddMilliseconds(timeoutMilliseconds);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await locator.CountAsync() > initialCount)
            {
                return true;
            }

            await Task.Delay(250, cancellationToken);
        }

        return false;
    }

    private async Task<AutomationResult> FailWithArtifactsAsync(
        string message,
        CancellationToken cancellationToken)
    {
        var screenshotPath = await CaptureScreenshotSafeAsync(cancellationToken);
        var tracePath = await StopTraceSafeAsync();
        return AutomationResult.Fail(message, screenshotPath, tracePath);
    }

    private async Task<string?> StopTraceSafeAsync()
    {
        try
        {
            return await _browserSession.StopTraceAsync(_options.TracesDirectory);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save Playwright trace.");
            return null;
        }
    }

    private static string ToXPathLiteral(string value)
    {
        if (!value.Contains('\''))
        {
            return $"'{value}'";
        }

        if (!value.Contains('"'))
        {
            return $"\"{value}\"";
        }

        var parts = value.Split('\'').Select(part => $"'{part}'");
        return "concat(" + string.Join(", \"'\", ", parts) + ")";
    }
}
