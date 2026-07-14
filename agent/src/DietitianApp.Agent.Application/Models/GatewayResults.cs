namespace DietitianApp.Agent.Application.Models;

public sealed record WhatsAppSessionResult(bool Success, string? ErrorCode = null, string? ErrorMessage = null);
public sealed record GroupVerificationResult(bool IsExactMatch, string? ErrorCode = null, string? ErrorMessage = null);
public sealed record SendMessageResult(bool Success, string? ErrorCode = null, string? ErrorMessage = null, string? ScreenshotPath = null, string? TracePath = null);
public sealed record CreateBatchRequest(IReadOnlyCollection<Guid> GroupIds, string MessageContent);
public sealed record BatchProgress(Guid ItemId, string GroupName, int Completed, int Total, string Status);
