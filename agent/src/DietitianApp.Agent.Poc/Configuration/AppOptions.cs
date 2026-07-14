namespace DietitianApp.Agent.Poc.Configuration;

public sealed class AppOptions
{
    public string WhatsAppUrl { get; init; } = "https://web.whatsapp.com/";

    public string UserDataDirectory { get; init; } = "./.profile/whatsapp";

    public string ScreenshotsDirectory { get; init; } = "./artifacts/screenshots";

    public string TracesDirectory { get; init; } = "./artifacts/traces";

    public int NavigationTimeoutSeconds { get; init; } = 60;

    public int LoginTimeoutSeconds { get; init; } = 180;

    public int ActionTimeoutSeconds { get; init; } = 20;

    public bool Headless { get; init; }
}
