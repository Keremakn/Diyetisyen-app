namespace DietitianApp.Agent.Infrastructure.Configuration;
public sealed class WhatsAppOptions
{
    public const string Section = "WhatsApp";
    public string Url { get; init; } = "https://web.whatsapp.com/";
    public int NavigationTimeoutSeconds { get; init; } = 60;
    public int LoginTimeoutSeconds { get; init; } = 180;
    public int ActionTimeoutSeconds { get; init; } = 20;
    public int MaxAttemptsPerItem { get; init; } = 1;
    public int MessageMaxLength { get; init; } = 4096;
    public bool Headless { get; init; }
}
