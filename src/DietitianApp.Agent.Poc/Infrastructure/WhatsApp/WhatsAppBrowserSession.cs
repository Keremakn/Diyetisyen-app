using DietitianApp.Agent.Poc.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace DietitianApp.Agent.Poc.Infrastructure.WhatsApp;

public sealed class WhatsAppBrowserSession : IAsyncDisposable
{
    private readonly AppOptions _options;
    private readonly ILogger<WhatsAppBrowserSession> _logger;
    private IPlaywright? _playwright;
    private IBrowserContext? _context;
    private IPage? _page;
    private bool _traceActive;

    public WhatsAppBrowserSession(IOptions<AppOptions> options, ILogger<WhatsAppBrowserSession> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public IPage? CurrentPage => _page;

    public async Task<IPage> GetPageAsync(CancellationToken cancellationToken)
    {
        if (_page is not null)
        {
            return _page;
        }

        Directory.CreateDirectory(_options.UserDataDirectory);

        _playwright = await Playwright.CreateAsync();
        _context = await _playwright.Chromium.LaunchPersistentContextAsync(
            _options.UserDataDirectory,
            new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = _options.Headless,
                Timeout = _options.NavigationTimeoutSeconds * 1_000,
                Args = ["--disable-blink-features=AutomationControlled"]
            });

        _context.SetDefaultTimeout(_options.ActionTimeoutSeconds * 1_000);
        _context.SetDefaultNavigationTimeout(_options.NavigationTimeoutSeconds * 1_000);

        await _context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
        _traceActive = true;

        _page = _context.Pages.Count > 0 ? _context.Pages[0] : await _context.NewPageAsync();
        await _page.GotoAsync(_options.WhatsAppUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = _options.NavigationTimeoutSeconds * 1_000
        });

        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("WhatsApp Web opened with persistent profile: {ProfilePath}", _options.UserDataDirectory);
        return _page;
    }

    public async Task<string?> StopTraceAsync(string tracesDirectory)
    {
        if (_context is null || !_traceActive)
        {
            return null;
        }

        Directory.CreateDirectory(tracesDirectory);
        var path = Path.Combine(tracesDirectory, $"trace-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.zip");
        await _context.Tracing.StopAsync(new TracingStopOptions { Path = path });
        _traceActive = false;
        return path;
    }

    public async ValueTask DisposeAsync()
    {
        if (_context is not null)
        {
            if (_traceActive)
            {
                try
                {
                    await _context.Tracing.StopAsync();
                }
                catch (PlaywrightException ex)
                {
                    _logger.LogDebug(ex, "Trace stop during shutdown failed.");
                }
            }

            await _context.CloseAsync();
        }

        _playwright?.Dispose();
    }
}
