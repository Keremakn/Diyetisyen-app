using DietitianApp.Agent.Poc.Application.Abstractions;
using DietitianApp.Agent.Poc.Configuration;
using DietitianApp.Agent.Poc.Infrastructure.Artifacts;
using DietitianApp.Agent.Poc.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

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
            var page = await _browserSession.GetPageAsync(cancellationToken);

            var searchBox = await FirstVisibleAsync(page, _selectors.SearchBoxSelectors);
            if (searchBox is null)
            {
                return AutomationResult.Fail("Arama kutusu bulunamadi. Mesaj gonderilmedi.");
            }

            await searchBox.ClickAsync();
            await ClearFocusedInputAsync(page);
            await searchBox.FillAsync(request.GroupName);
            await page.WaitForTimeoutAsync(1_000);

            var exactGroup = page.Locator($"xpath=//span[@title={ToXPathLiteral(request.GroupName)}]").First;
            if (!await WaitUntilVisibleAsync(exactGroup, _options.ActionTimeoutSeconds * 1_000))
            {
                return AutomationResult.Fail($"Tam eslesen grup bulunamadi: {request.GroupName}");
            }

            await exactGroup.ClickAsync();

            var headerTitle = page.Locator(_selectors.ChatTitleSelector).First;
            var selectedTitle = await headerTitle.GetAttributeAsync("title", new LocatorGetAttributeOptions
            {
                Timeout = _options.ActionTimeoutSeconds * 1_000
            });

            if (!string.Equals(selectedTitle, request.GroupName, StringComparison.Ordinal))
            {
                return AutomationResult.Fail($"Secilen sohbet tam eslesmedi. Beklenen: '{request.GroupName}', bulunan: '{selectedTitle}'.");
            }

            var messageBox = await FirstVisibleAsync(page, _selectors.MessageBoxSelectors);
            if (messageBox is null)
            {
                return AutomationResult.Fail("Mesaj kutusu bulunamadi. Mesaj gonderilmedi.");
            }

            await messageBox.ClickAsync();
            await messageBox.FillAsync(request.Message);
            await page.Keyboard.PressAsync("Enter");

            _logger.LogInformation("Message sent to exact test group {GroupName}.", request.GroupName);
            return AutomationResult.Ok($"Mesaj gonderildi: {request.GroupName}");
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

    private async Task<ILocator?> FirstVisibleAsync(IPage page, IReadOnlyCollection<string> selectors)
    {
        foreach (var selector in selectors)
        {
            try
            {
                var locator = page.Locator(selector).First;
                if (await WaitUntilVisibleAsync(locator, _options.ActionTimeoutSeconds * 1_000))
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
