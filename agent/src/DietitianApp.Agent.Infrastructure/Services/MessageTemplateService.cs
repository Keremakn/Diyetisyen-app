using DietitianApp.Agent.Application.Abstractions;
using DietitianApp.Agent.Domain.Entities;
using DietitianApp.Agent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DietitianApp.Agent.Infrastructure.Services;
public sealed class MessageTemplateService(AgentDbContext db, IClock clock) : IMessageTemplateService
{
    public async Task<IReadOnlyList<MessageTemplate>> GetAllAsync(CancellationToken token)=>await db.MessageTemplates.OrderBy(x=>x.Name).ToListAsync(token);
    public async Task<MessageTemplate> SaveAsync(MessageTemplate item,CancellationToken token)
    { if(string.IsNullOrWhiteSpace(item.Content))throw new ArgumentException("Şablon içeriği boş olamaz.");var now=clock.UtcNow;item.UpdatedAtUtc=now;if(item.Id==Guid.Empty){item.Id=Guid.NewGuid();item.CreatedAtUtc=now;db.MessageTemplates.Add(item);}await db.SaveChangesAsync(token);return item; }
}
