namespace Property.Application.Common.Interfaces;

/// <summary>Dispatches pending outbox messages (used by property-worker).</summary>
public interface IOutboxDispatcher
{
    /// <summary>Processes up to <paramref name="batchSize"/> due messages. Returns how many were claimed.</summary>
    Task<int> DispatchPendingAsync(int batchSize, CancellationToken cancellationToken = default);
}
