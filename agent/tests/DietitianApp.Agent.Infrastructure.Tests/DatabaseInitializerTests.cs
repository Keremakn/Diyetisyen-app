using DietitianApp.Agent.Domain.Entities;
using DietitianApp.Agent.Domain.Enums;
using DietitianApp.Agent.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DietitianApp.Agent.Infrastructure.Tests;

public sealed class DatabaseInitializerTests
{
    [Fact]
    public async Task Initialize_recovers_interrupted_processing_batches()
    {
        await using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source=dietitian-app-test-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AgentDbContext(options);
        await db.Database.MigrateAsync();
        var groups = new[]
        {
            new WhatsAppGroup { Id = Guid.NewGuid(), ExternalName = "done", DisplayName = "done", IsActive = true, IsVerified = true },
            new WhatsAppGroup { Id = Guid.NewGuid(), ExternalName = "working", DisplayName = "working", IsActive = true, IsVerified = true },
            new WhatsAppGroup { Id = Guid.NewGuid(), ExternalName = "pending", DisplayName = "pending", IsActive = true, IsVerified = true }
        };
        db.WhatsAppGroups.AddRange(groups);
        var batch = new SendBatch
        {
            Id = Guid.NewGuid(),
            MessageContent = "test",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            StartedAtUtc = DateTimeOffset.UtcNow,
            Status = SendBatchStatus.Processing,
            TotalCount = 3,
            Items =
            [
                new SendBatchItem { Id = Guid.NewGuid(), WhatsAppGroupId = groups[0].Id, GroupNameSnapshot = "done", Status = SendItemStatus.Succeeded, CompletedAtUtc = DateTimeOffset.UtcNow },
                new SendBatchItem { Id = Guid.NewGuid(), WhatsAppGroupId = groups[1].Id, GroupNameSnapshot = "working", Status = SendItemStatus.Processing, StartedAtUtc = DateTimeOffset.UtcNow },
                new SendBatchItem { Id = Guid.NewGuid(), WhatsAppGroupId = groups[2].Id, GroupNameSnapshot = "pending", Status = SendItemStatus.Pending }
            ]
        };
        db.SendBatches.Add(batch);
        await db.SaveChangesAsync();

        var initializer = new DatabaseInitializer(db, NullLogger<DatabaseInitializer>.Instance);
        await initializer.InitializeAsync();

        batch.Status.Should().Be(SendBatchStatus.CompletedWithErrors);
        batch.SuccessCount.Should().Be(1);
        batch.FailureCount.Should().Be(1);
        batch.CancelledCount.Should().Be(1);
        batch.Items.Single(x => x.GroupNameSnapshot == "working").Status.Should().Be(SendItemStatus.Failed);
        batch.Items.Single(x => x.GroupNameSnapshot == "pending").Status.Should().Be(SendItemStatus.Cancelled);

        await db.DisposeAsync();
    }
}
