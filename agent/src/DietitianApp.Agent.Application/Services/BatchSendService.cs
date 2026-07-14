using DietitianApp.Agent.Application.Abstractions;
using DietitianApp.Agent.Application.Common;
using DietitianApp.Agent.Application.Models;
using DietitianApp.Agent.Domain.Entities;
using DietitianApp.Agent.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DietitianApp.Agent.Application.Services;

public sealed class BatchSendService : IBatchSendService
{
    private readonly IApplicationDbContext _db;
    private readonly IWhatsAppGateway _gateway;
    private readonly IClock _clock;

    public BatchSendService(IApplicationDbContext db, IWhatsAppGateway gateway, IClock clock)
    {
        _db = db;
        _gateway = gateway;
        _clock = clock;
    }

    public async Task<SendBatch> CreateAsync(CreateBatchRequest request, CancellationToken cancellationToken)
    {
        if (request.GroupIds.Count == 0)
            throw new ApplicationValidationException("En az bir grup secilmelidir.");
        if (string.IsNullOrWhiteSpace(request.MessageContent))
            throw new ApplicationValidationException("Mesaj bos olamaz.");

        var distinctIds = request.GroupIds.Distinct().ToArray();
        var groups = await _db.WhatsAppGroups.Where(x => distinctIds.Contains(x.Id)).ToListAsync(cancellationToken);
        if (groups.Count != distinctIds.Length)
            throw new ApplicationValidationException("Secilen gruplardan biri bulunamadi.");
        if (groups.Any(x => !x.IsActive))
            throw new ApplicationValidationException("Pasif grup gonderime alinamaz.");
        if (groups.Any(x => !x.IsVerified))
            throw new ApplicationValidationException("Dogrulanmamis grup gonderime alinamaz.");

        var now = _clock.UtcNow;
        var batch = new SendBatch
        {
            Id = Guid.NewGuid(),
            MessageContent = request.MessageContent.Trim(),
            CreatedAtUtc = now,
            Status = SendBatchStatus.Pending,
            TotalCount = groups.Count,
            Items = groups.Select(group => new SendBatchItem
            {
                Id = Guid.NewGuid(),
                WhatsAppGroupId = group.Id,
                GroupNameSnapshot = group.ExternalName,
                Status = SendItemStatus.Pending
            }).ToList()
        };

        _db.SendBatches.Add(batch);
        await _db.SaveChangesAsync(cancellationToken);
        return batch;
    }

    public async Task<SendBatch> StartAsync(Guid batchId, IProgress<BatchProgress>? progress, CancellationToken cancellationToken)
    {
        var batch = await LoadAsync(batchId, cancellationToken);
        if (batch.Status == SendBatchStatus.Processing)
            throw new ApplicationValidationException("Bu gonderim zaten calisiyor.");
        if (batch.Status is not SendBatchStatus.Pending)
            throw new ApplicationValidationException("Yalnizca bekleyen gonderim baslatilabilir.");

        return await ProcessAsync(batch, batch.Items.Where(x => x.Status == SendItemStatus.Pending).ToList(), progress, cancellationToken);
    }

    public async Task<SendBatch> RetryFailuresAsync(Guid batchId, IProgress<BatchProgress>? progress, CancellationToken cancellationToken)
    {
        var source = await LoadAsync(batchId, cancellationToken);
        var failed = source.Items.Where(x => x.Status == SendItemStatus.Failed).ToList();
        if (failed.Count == 0)
            throw new ApplicationValidationException("Yeniden denenecek basarisiz oge yok.");

        return await CreateAndStartRetryAsync(source.MessageContent, failed, progress, cancellationToken);
    }

    public async Task<SendBatch> RetryUnsuccessfulAsync(Guid batchId, IProgress<BatchProgress>? progress, CancellationToken cancellationToken)
    {
        var source = await LoadAsync(batchId, cancellationToken);
        var retryable = source.Items
            .Where(x => x.Status is SendItemStatus.Failed or SendItemStatus.Cancelled)
            .ToList();
        if (retryable.Count == 0)
            throw new ApplicationValidationException("Yeniden denenecek hatali veya iptal edilmis oge yok.");

        return await CreateAndStartRetryAsync(source.MessageContent, retryable, progress, cancellationToken);
    }

    private async Task<SendBatch> CreateAndStartRetryAsync(string message, IReadOnlyCollection<SendBatchItem> items, IProgress<BatchProgress>? progress, CancellationToken cancellationToken)
    {
        var retry = await CreateAsync(new CreateBatchRequest(items.Select(x => x.WhatsAppGroupId).ToArray(), message), cancellationToken);
        return await StartAsync(retry.Id, progress, cancellationToken);
    }

    private async Task<SendBatch> ProcessAsync(SendBatch batch, List<SendBatchItem> items, IProgress<BatchProgress>? progress, CancellationToken cancellationToken)
    {
        batch.Status = SendBatchStatus.Processing;
        batch.StartedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(CancellationToken.None);

        var session = await _gateway.EnsureSessionAsync(cancellationToken);
        if (!session.Success)
        {
            batch.Status = SendBatchStatus.Failed;
            batch.CompletedAtUtc = _clock.UtcNow;
            foreach (var item in items)
                Fail(item, session.ErrorCode ?? "SESSION", session.ErrorMessage ?? "WhatsApp oturumu hazir degil.");
            Recalculate(batch);
            await _db.SaveChangesAsync(CancellationToken.None);
            return batch;
        }

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            if (cancellationToken.IsCancellationRequested)
            {
                foreach (var pending in items.Skip(index))
                    Cancel(pending);
                break;
            }

            item.Status = SendItemStatus.Processing;
            item.StartedAtUtc = _clock.UtcNow;
            item.AttemptCount++;
            await _db.SaveChangesAsync(CancellationToken.None);

            try
            {
                var result = await _gateway.SendMessageAsync(item.GroupNameSnapshot, batch.MessageContent, CancellationToken.None);
                if (result.Success)
                {
                    item.Status = SendItemStatus.Succeeded;
                    item.CompletedAtUtc = _clock.UtcNow;
                }
                else
                {
                    Fail(item, result.ErrorCode ?? "SEND_FAILED", result.ErrorMessage ?? "Mesaj gonderilemedi.");
                    item.ScreenshotPath = result.ScreenshotPath;
                    item.TracePath = result.TracePath;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Cancel(item);
                foreach (var pending in items.Skip(index + 1))
                    Cancel(pending);
                break;
            }
            catch (Exception ex)
            {
                Fail(item, "UNEXPECTED", ex.Message);
            }

            progress?.Report(new BatchProgress(item.Id, item.GroupNameSnapshot, index + 1, items.Count, item.Status.ToString()));
            await _db.SaveChangesAsync(CancellationToken.None);
        }

        Recalculate(batch);
        batch.CompletedAtUtc = _clock.UtcNow;
        batch.Status = batch.CancelledCount > 0
            ? SendBatchStatus.Cancelled
            : batch.FailureCount > 0 ? SendBatchStatus.CompletedWithErrors : SendBatchStatus.Completed;
        await _db.SaveChangesAsync(CancellationToken.None);
        return batch;
    }

    private async Task<SendBatch> LoadAsync(Guid id, CancellationToken token) =>
        await _db.SendBatches.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, token)
        ?? throw new ApplicationValidationException("Gonderim bulunamadi.");

    private void Fail(SendBatchItem item, string code, string message)
    {
        item.Status = SendItemStatus.Failed;
        item.ErrorCode = code;
        item.ErrorMessage = message;
        item.CompletedAtUtc = _clock.UtcNow;
    }

    private void Cancel(SendBatchItem item)
    {
        item.Status = SendItemStatus.Cancelled;
        item.CompletedAtUtc = _clock.UtcNow;
    }

    private static void Recalculate(SendBatch batch)
    {
        batch.TotalCount = batch.Items.Count;
        batch.SuccessCount = batch.Items.Count(x => x.Status == SendItemStatus.Succeeded);
        batch.FailureCount = batch.Items.Count(x => x.Status == SendItemStatus.Failed);
        batch.CancelledCount = batch.Items.Count(x => x.Status == SendItemStatus.Cancelled);
    }
}
