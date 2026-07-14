using DietitianApp.Agent.Domain.Entities;

namespace DietitianApp.Agent.Application.Abstractions;

public interface IMessageTemplateService
{
    Task<IReadOnlyList<MessageTemplate>> GetAllAsync(CancellationToken cancellationToken);
    Task<MessageTemplate> SaveAsync(MessageTemplate template, CancellationToken cancellationToken);
}
