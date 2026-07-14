using DietitianApp.Agent.Application.Abstractions;
using DietitianApp.Agent.Domain.Entities;
using DietitianApp.Agent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DietitianApp.Agent.Infrastructure.Services;
public sealed class GroupService(AgentDbContext db, IWhatsAppGateway gateway, IClock clock) : IGroupService
{
    public async Task<IReadOnlyList<WhatsAppGroup>> GetAllAsync(CancellationToken token) => await db.WhatsAppGroups.OrderBy(x => x.DisplayName).ToListAsync(token);
    public async Task<WhatsAppGroup> SaveAsync(WhatsAppGroup group, CancellationToken token)
    {
        var now=clock.UtcNow; group.ExternalName=group.ExternalName.Trim(); group.DisplayName=group.DisplayName.Trim(); group.UpdatedAtUtc=now;
        if(group.Id==Guid.Empty){group.Id=Guid.NewGuid();group.CreatedAtUtc=now;db.WhatsAppGroups.Add(group);} await db.SaveChangesAsync(token); return group;
    }
    public async Task<bool> VerifyAsync(Guid id, CancellationToken token)
    {
        var group=await db.WhatsAppGroups.SingleAsync(x=>x.Id==id,token); var result=await gateway.VerifyGroupAsync(group.ExternalName,token);
        group.IsVerified=result.IsExactMatch; group.VerifiedAtUtc=result.IsExactMatch?clock.UtcNow:null; group.UpdatedAtUtc=clock.UtcNow; await db.SaveChangesAsync(token); return result.IsExactMatch;
    }
}
