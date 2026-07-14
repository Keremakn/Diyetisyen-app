using DietitianApp.Agent.Domain.Enums;

namespace DietitianApp.Agent.Domain.Entities;

public sealed class SendBatch
{
    public Guid Id { get; set; }
    public string MessageContent { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public SendBatchStatus Status { get; set; } = SendBatchStatus.Draft;
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int CancelledCount { get; set; }
    public ICollection<SendBatchItem> Items { get; set; } = new List<SendBatchItem>();
}
