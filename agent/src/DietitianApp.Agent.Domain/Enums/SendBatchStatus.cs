namespace DietitianApp.Agent.Domain.Enums;

public enum SendBatchStatus
{
    Draft,
    Pending,
    Processing,
    Completed,
    CompletedWithErrors,
    Cancelled,
    Failed
}
