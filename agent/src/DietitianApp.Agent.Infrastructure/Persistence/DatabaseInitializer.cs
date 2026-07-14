using DietitianApp.Agent.Domain.Entities;
using DietitianApp.Agent.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DietitianApp.Agent.Infrastructure.Persistence;

public sealed class DatabaseInitializer(AgentDbContext db, ILogger<DatabaseInitializer> log)
{
    public async Task InitializeAsync(CancellationToken token = default)
    {
        await db.Database.MigrateAsync(token);
        await RecoverInterruptedBatchesAsync(token);
        await SeedTemplatesAsync(token);
    }

    private async Task RecoverInterruptedBatchesAsync(CancellationToken token)
    {
        var interrupted = await db.SendBatches
            .Include(x => x.Items)
            .Where(x => x.Status == SendBatchStatus.Processing)
            .ToListAsync(token);

        if (interrupted.Count == 0)
            return;

        var now = DateTimeOffset.UtcNow;
        foreach (var batch in interrupted)
        {
            foreach (var item in batch.Items)
            {
                if (item.Status == SendItemStatus.Succeeded || item.Status == SendItemStatus.Failed || item.Status == SendItemStatus.Cancelled)
                    continue;

                item.CompletedAtUtc = now;
                if (item.Status == SendItemStatus.Processing)
                {
                    item.Status = SendItemStatus.Failed;
                    item.ErrorCode = "INTERRUPTED";
                    item.ErrorMessage = "Uygulama kapanmasi nedeniyle gonderim sonucu dogrulanamadi.";
                }
                else if (item.Status == SendItemStatus.Pending)
                {
                    item.Status = SendItemStatus.Cancelled;
                    item.ErrorCode = "INTERRUPTED";
                    item.ErrorMessage = "Uygulama kapanmasi nedeniyle bekleyen gonderim iptal edildi.";
                }
            }

            batch.CompletedAtUtc = now;
            batch.TotalCount = batch.Items.Count;
            batch.SuccessCount = batch.Items.Count(x => x.Status == SendItemStatus.Succeeded);
            batch.FailureCount = batch.Items.Count(x => x.Status == SendItemStatus.Failed);
            batch.CancelledCount = batch.Items.Count(x => x.Status == SendItemStatus.Cancelled);
            batch.Status = batch.FailureCount > 0
                ? SendBatchStatus.CompletedWithErrors
                : batch.CancelledCount > 0 ? SendBatchStatus.Cancelled : SendBatchStatus.Failed;

            log.LogWarning("Recovered interrupted batch {BatchId}. Success={SuccessCount}, Failure={FailureCount}, Cancelled={CancelledCount}.",
                batch.Id, batch.SuccessCount, batch.FailureCount, batch.CancelledCount);
        }

        await db.SaveChangesAsync(token);
    }

    private async Task SeedTemplatesAsync(CancellationToken token)
    {
        var now = DateTimeOffset.UtcNow;
        if (!await db.MessageTemplates.AnyAsync(x => x.Id == Guid.Parse("4D31EF85-CA92-4EBE-AF79-076703821101"), token))
        {
            db.MessageTemplates.Add(new MessageTemplate
            {
                Id = Guid.Parse("4D31EF85-CA92-4EBE-AF79-076703821101"),
                Name = "Gunaydin Mesaji",
                Content = "Gunaydin. Bugun kendin icin yapacagin her saglikli secim hedeflerine attigin degerli bir adim olsun.",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        if (!await db.MessageTemplates.AnyAsync(x => x.Id == Guid.Parse("4D31EF85-CA92-4EBE-AF79-076703821102"), token))
        {
            db.MessageTemplates.Add(new MessageTemplate
            {
                Id = Guid.Parse("4D31EF85-CA92-4EBE-AF79-076703821102"),
                Name = "Aksam Ogun Kontrolu",
                Content = "Iyi aksamlar. Bugunku ogunlerini ve su tuketimini kisaca degerlendirebilir misin?",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        await db.SaveChangesAsync(token);
    }
}
