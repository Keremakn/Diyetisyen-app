namespace DietitianApp.Agent.Infrastructure.Paths;
public sealed class AppPathProvider : IAppPathProvider
{
    public string Root { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DietitianApp");
    public string DatabasePath => Path.Combine(Root, "agent.db");
    public string BrowserProfilePath => Path.Combine(Root, "BrowserProfile");
    public string LogsPath => Path.Combine(Root, "Logs");
    public string ArtifactsPath => Path.Combine(Root, "Artifacts");
    public string ScreenshotsPath => Path.Combine(ArtifactsPath, "screenshots");
    public string TracesPath => Path.Combine(ArtifactsPath, "traces");
    public void EnsureDirectories()
    { foreach (var path in new[] { Root, BrowserProfilePath, LogsPath, ScreenshotsPath, TracesPath }) Directory.CreateDirectory(path); }
}
