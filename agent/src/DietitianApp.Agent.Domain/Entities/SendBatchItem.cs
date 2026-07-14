using DietitianApp.Agent.Domain.Enums;

namespace DietitianApp.Agent.Domain.Entities;

public sealed class SendBatchItem
{
    public Guid Id { get; set; }
    public Guid SendBatchId { get; set; }
    public Guid WhatsAppGroupId { get; set; }
    public string GroupNameSnapshot { get; set; } = string.Empty;
    public SendItemStatus Status { get; set; } = SendItemStatus.Pending;
    public int AttemptCount { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ScreenshotPath { get; set; }
    public string? TracePath { get; set; }
    public SendBatch? SendBatch { get; set; }
    public WhatsAppGroup? WhatsAppGroup { get; set; }
}
