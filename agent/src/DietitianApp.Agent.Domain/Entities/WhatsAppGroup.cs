namespace DietitianApp.Agent.Domain.Entities;

public sealed class WhatsAppGroup
{
    public Guid Id { get; set; }
    public string ExternalName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Prefix { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsVerified { get; set; }
    public DateTimeOffset? VerifiedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
