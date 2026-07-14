namespace DietitianApp.Agent.Poc.Models;

public sealed record AutomationResult(
    bool Success,
    string Message,
    string? ScreenshotPath = null,
    string? TracePath = null)
{
    public static AutomationResult Ok(string message) => new(true, message);

    public static AutomationResult Fail(string message, string? screenshotPath = null, string? tracePath = null) =>
        new(false, message, screenshotPath, tracePath);
}
