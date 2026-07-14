namespace DietitianApp.Agent.Infrastructure.Paths;
public interface IAppPathProvider
{
    string Root { get; } string DatabasePath { get; } string BrowserProfilePath { get; }
    string LogsPath { get; } string ArtifactsPath { get; } string ScreenshotsPath { get; } string TracesPath { get; }
    void EnsureDirectories();
}
