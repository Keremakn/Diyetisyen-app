using DietitianApp.Agent.Application.Abstractions;
using DietitianApp.Agent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DietitianApp.Agent.Infrastructure.Persistence;
public sealed class AgentDbContext(DbContextOptions<AgentDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<WhatsAppGroup> WhatsAppGroups => Set<WhatsAppGroup>();
    public DbSet<MessageTemplate> MessageTemplates => Set<MessageTemplate>();
    public DbSet<SendBatch> SendBatches => Set<SendBatch>();
    public DbSet<SendBatchItem> SendBatchItems => Set<SendBatchItem>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<WhatsAppGroup>(e => { e.HasKey(x => x.Id); e.Property(x => x.ExternalName).HasMaxLength(256).IsRequired(); e.HasIndex(x => x.ExternalName).IsUnique(); e.Property(x => x.DisplayName).HasMaxLength(256).IsRequired(); });
        b.Entity<MessageTemplate>(e => { e.HasKey(x => x.Id); e.Property(x => x.Name).HasMaxLength(128).IsRequired(); e.Property(x => x.Content).HasMaxLength(4096).IsRequired(); e.HasIndex(x => x.Name).IsUnique(); });
        b.Entity<SendBatch>(e => { e.HasKey(x => x.Id); e.Property(x => x.MessageContent).HasMaxLength(4096).IsRequired(); e.HasMany(x => x.Items).WithOne(x => x.SendBatch).HasForeignKey(x => x.SendBatchId).OnDelete(DeleteBehavior.Cascade); });
        b.Entity<SendBatchItem>(e => { e.HasKey(x => x.Id); e.Property(x => x.GroupNameSnapshot).HasMaxLength(256).IsRequired(); e.HasOne(x => x.WhatsAppGroup).WithMany().HasForeignKey(x => x.WhatsAppGroupId).OnDelete(DeleteBehavior.Restrict); });
    }
}
