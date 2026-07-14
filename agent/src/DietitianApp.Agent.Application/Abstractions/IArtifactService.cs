namespace DietitianApp.Agent.Application.Abstractions;

public interface IArtifactService
{
    Task<string?> CaptureScreenshotAsync(CancellationToken cancellationToken);
    Task<string?> SaveTraceAsync(CancellationToken cancellationToken);
}
