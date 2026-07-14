using DietitianApp.Agent.Poc.Models;

namespace DietitianApp.Agent.Poc.Application.Abstractions;

public interface IWhatsAppAutomationService
{
    Task<AutomationResult> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken);
}
