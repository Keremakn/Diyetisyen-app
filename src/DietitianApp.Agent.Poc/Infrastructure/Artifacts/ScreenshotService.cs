using Microsoft.Playwright;

namespace DietitianApp.Agent.Poc.Infrastructure.Artifacts;

public sealed class ScreenshotService
{
    public async Task<string?> CaptureAsync(IPage? page, string screenshotsDirectory, CancellationToken cancellationToken)
    {
        if (page is null)
        {
            return null;
        }

        Directory.CreateDirectory(screenshotsDirectory);
        var path = Path.Combine(screenshotsDirectory, $"failure-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.png");
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = path,
            FullPage = true,
            Timeout = 5_000
        });

        cancellationToken.ThrowIfCancellationRequested();
        return path;
    }
}
