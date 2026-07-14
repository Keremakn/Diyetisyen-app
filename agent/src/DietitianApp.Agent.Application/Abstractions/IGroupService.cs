using DietitianApp.Agent.Domain.Entities;

namespace DietitianApp.Agent.Application.Abstractions;

public interface IGroupService
{
    Task<IReadOnlyList<WhatsAppGroup>> GetAllAsync(CancellationToken cancellationToken);
    Task<WhatsAppGroup> SaveAsync(WhatsAppGroup group, CancellationToken cancellationToken);
    Task<bool> VerifyAsync(Guid groupId, CancellationToken cancellationToken);
}
