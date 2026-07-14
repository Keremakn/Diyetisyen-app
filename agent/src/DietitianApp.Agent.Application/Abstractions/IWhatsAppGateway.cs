using DietitianApp.Agent.Application.Models;

namespace DietitianApp.Agent.Application.Abstractions;

public interface IWhatsAppGateway
{
    Task<WhatsAppSessionResult> EnsureSessionAsync(CancellationToken cancellationToken);
    Task<GroupVerificationResult> VerifyGroupAsync(string exactGroupName, CancellationToken cancellationToken);
    Task<SendMessageResult> SendMessageAsync(string exactGroupName, string message, CancellationToken cancellationToken);
}
