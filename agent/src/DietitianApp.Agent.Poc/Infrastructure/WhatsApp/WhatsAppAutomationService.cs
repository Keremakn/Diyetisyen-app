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
            await ClearFocusedInputAsync(page);
            await messageBox.FillAsync(request.Message);
            if (!await WaitForComposerTextAsync(messageBox, request.Message, 3_000, cancellationToken))
            {
                return await FailWithArtifactsAsync(
                    "Mesaj kutusu temizlenip hedef mesajla doldurulamadi. Mesaj gonderilmedi.",
                    cancellationToken);
            }

            var sentMessage = page.Locator(
                $"xpath=//*[@id='main']//*[@data-testid='msg-container' " +
                $"and .//span[@aria-label='Siz:' or @aria-label='You:'] " +
                $"and .//*[@data-testid='selectable-text' and normalize-space(.)={ToXPathLiteral(request.Message)}]]");
            var sentMessageCountBeforeSend = await sentMessage.CountAsync();

            var deliveredMessage = page.Locator(
                $"xpath=//*[@id='main']//*[@data-testid='msg-container' " +
                $"and .//span[@aria-label='Siz:' or @aria-label='You:'] " +
                $"and .//*[@data-testid='selectable-text' and normalize-space(.)={ToXPathLiteral(request.Message)}] " +
                $"and .//*[local-name()='title' and " +
                $"(.='wds-ic-read' or .='wds-ic-check' or .='msg-dblcheck' or .='msg-check')]]");
            var deliveredMessageCountBeforeSend = await deliveredMessage.CountAsync();

            var sendButton = await FirstVisibleAsync(
                page,
                [
                    "footer button span[data-icon='send'] >> xpath=ancestor::button[1]",
                    "footer [role='button'] span[data-icon='send'] >> xpath=ancestor::*[@role='button'][1]",
                    "footer button[aria-label='Gönder']",
                    "footer button[aria-label='Send']",
                    "footer span[data-icon='send']"
                ],
                3_000);

            if (sendButton is not null)
            {
                await sendButton.ClickAsync();
            }
            else
            {
                await page.Keyboard.PressAsync("Enter");
            }

            if (!await WaitForCountIncreaseAsync(
                    sentMessage,
                    sentMessageCountBeforeSend,
                    10_000,
                    cancellationToken))
            {
                return await FailWithArtifactsAsync(
                    "Mesaj gonderme islemi tetiklendi ancak giden mesaj balonunda dogrulanamadi.",
                    cancellationToken);
            }

            if (!await WaitForDeliveredOrRetryAsync(
                    page,
                    request.Message,
                    deliveredMessage,
                    deliveredMessageCountBeforeSend,
                    cancellationToken))
            {
                return await FailWithArtifactsAsync(
                    "Mesaj WhatsApp'ta olustu ancak tek tik/cift tik durumuna gecmedi. Beklemede veya gonderilemedi olabilir.",
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

    private static async Task<bool> WaitForComposerTextAsync(
        ILocator messageBox,
        string expectedMessage,
        int timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.AddMilliseconds(timeoutMilliseconds);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentText = await messageBox.InnerTextAsync();
            if (string.Equals(currentText.Trim(), expectedMessage, StringComparison.Ordinal))
            {
                return true;
            }

            await Task.Delay(100, cancellationToken);
        }

        return false;
    }

    private async Task<bool> WaitForDeliveredOrRetryAsync(
        IPage page,
        string message,
        ILocator deliveredMessage,
        int deliveredMessageCountBeforeSend,
        CancellationToken cancellationToken)
    {
        var retryAttempts = 0;
        var deadline = DateTimeOffset.UtcNow.AddSeconds(45);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await deliveredMessage.CountAsync() > deliveredMessageCountBeforeSend)
            {
                return true;
            }

            if (retryAttempts < 2 && await RetryFailedMessageIfVisibleAsync(page, message, cancellationToken))
            {
                retryAttempts++;
                _logger.LogInformation("WhatsApp send failed indicator detected. Retry attempt {Attempt}.", retryAttempts);
            }

            await Task.Delay(500, cancellationToken);
        }

        return false;
    }

    private async Task<bool> RetryFailedMessageIfVisibleAsync(
        IPage page,
        string message,
        CancellationToken cancellationToken)
    {
        var failedMessage = page.Locator(
            $"xpath=//*[@id='main']//*[@data-testid='msg-container' " +
            $"and .//span[@aria-label='Siz:' or @aria-label='You:'] " +
            $"and .//*[@data-testid='selectable-text' and normalize-space(.)={ToXPathLiteral(message)}] " +
            $"and (.//*[local-name()='title' and " +
            $"(contains(translate(., 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'error') " +
            $"or contains(translate(., 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'failed') " +
            $"or contains(., 'hata'))] " +
            $"or .//*[contains(translate(@aria-label, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'failed') " +
            $"or contains(@aria-label, 'gonderilemedi') " +
            $"or contains(@aria-label, 'gönderilemedi') " +
            $"or contains(@aria-label, 'hata')])]");

        if (!await WaitUntilVisibleAsync(failedMessage.Last, 250))
        {
            return false;
        }

        await failedMessage.Last.ClickAsync();

        var retryOption = await FirstVisibleAsync(
            page,
            [
                "text=/Tekrar dene/i",
                "text=/Tekrar gönder/i",
                "text=/Yeniden gönder/i",
                "text=/Yeniden dene/i",
                "text=/Retry/i",
                "text=/Resend/i",
                "[role='button']:has-text('Tekrar dene')",
                "[role='button']:has-text('Tekrar gönder')",
                "[role='button']:has-text('Yeniden gönder')",
                "[role='button']:has-text('Yeniden dene')",
                "[role='button']:has-text('Retry')",
                "[role='button']:has-text('Resend')",
                "[role='menuitem']:has-text('Tekrar dene')",
                "[role='menuitem']:has-text('Tekrar gönder')",
                "[role='menuitem']:has-text('Yeniden gönder')",
                "[role='menuitem']:has-text('Yeniden dene')",
                "[role='menuitem']:has-text('Retry')",
                "[role='menuitem']:has-text('Resend')"
            ],
            3_000);

        if (retryOption is null)
        {
            return false;
        }

        await retryOption.ClickAsync();
        await Task.Delay(500, cancellationToken);
        return true;
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
