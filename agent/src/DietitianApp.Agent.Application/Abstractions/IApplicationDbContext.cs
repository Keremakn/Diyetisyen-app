using DietitianApp.Agent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DietitianApp.Agent.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<WhatsAppGroup> WhatsAppGroups { get; }
    DbSet<MessageTemplate> MessageTemplates { get; }
    DbSet<SendBatch> SendBatches { get; }
    DbSet<SendBatchItem> SendBatchItems { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
