using DietitianApp.Agent.Application.Models;
using DietitianApp.Agent.Domain.Entities;

namespace DietitianApp.Agent.Application.Abstractions;

public interface IBatchSendService
{
    Task<SendBatch> CreateAsync(CreateBatchRequest request, CancellationToken cancellationToken);
    Task<SendBatch> StartAsync(Guid batchId, IProgress<BatchProgress>? progress, CancellationToken cancellationToken);
    Task<SendBatch> RetryFailuresAsync(Guid batchId, IProgress<BatchProgress>? progress, CancellationToken cancellationToken);
    Task<SendBatch> RetryUnsuccessfulAsync(Guid batchId, IProgress<BatchProgress>? progress, CancellationToken cancellationToken);
}
