using DietitianApp.Agent.Poc.Application.Abstractions;
using DietitianApp.Agent.Poc.Configuration;
using DietitianApp.Agent.Poc.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace DietitianApp.Agent.Poc.Infrastructure.WhatsApp;

public sealed class WhatsAppSessionService : IWhatsAppSessionService
{
    private readonly WhatsAppBrowserSession _browserSession;
    private readonly AppOptions _options;
    private readonly WhatsAppSelectorsOptions _selectors;
    private readonly ILogger<WhatsAppSessionService> _logger;

    public WhatsAppSessionService(
        WhatsAppBrowserSession browserSession,
        IOptions<AppOptions> options,
        IOptions<WhatsAppSelectorsOptions> selectors,
        ILogger<WhatsAppSessionService> logger)
    {
        _browserSession = browserSession;
        _options = options.Value;
        _selectors = selectors.Value;
        _logger = logger;
    }

    public async Task<SessionStatus> EnsureSessionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var page = await _browserSession.GetPageAsync(cancellationToken);
            if (await AnyVisibleAsync(page, _selectors.CaptchaIndicators))
            {
                _logger.LogWarning("CAPTCHA indicator detected.");
                return SessionStatus.CaptchaDetected;
            }

            if (await AnyVisibleAsync(page, _selectors.LoginReadySelectors))
            {
                _logger.LogInformation("Existing WhatsApp session is authenticated.");
                return SessionStatus.Authenticated;
            }

            if (await AnyVisibleAsync(page, _selectors.QrLoginSelectors))
            {
                _logger.LogInformation("QR login required. Waiting for user to authenticate.");
            }

            var deadline = DateTimeOffset.UtcNow.AddSeconds(_options.LoginTimeoutSeconds);
            while (DateTimeOffset.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (await AnyVisibleAsync(page, _selectors.CaptchaIndicators))
                {
                    return SessionStatus.CaptchaDetected;
                }

                if (await AnyVisibleAsync(page, _selectors.LoginReadySelectors))
                {
                    _logger.LogInformation("WhatsApp session authenticated.");
                    return SessionStatus.Authenticated;
                }

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }

            return SessionStatus.RequiresQrLogin;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session check failed.");
            return SessionStatus.Failed;
        }
    }

    private static async Task<bool> AnyVisibleAsync(IPage page, IReadOnlyCollection<string> selectors)
    {
        foreach (var selector in selectors)
        {
            try
            {
                await page.Locator(selector).First.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 1_000
                });
                return true;
            }
            catch (TimeoutException)
            {
                // Selector candidate was not visible within the short probe window.
            }
            catch (PlaywrightException)
            {
                // Selector candidates are intentionally tolerant because WhatsApp Web changes often.
            }
        }

        return false;
    }
}
