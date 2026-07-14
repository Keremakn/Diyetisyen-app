using DietitianApp.Agent.Poc.Models;

namespace DietitianApp.Agent.Poc.Application.Abstractions;

public interface IWhatsAppSessionService
{
    Task<SessionStatus> EnsureSessionAsync(CancellationToken cancellationToken);
}
