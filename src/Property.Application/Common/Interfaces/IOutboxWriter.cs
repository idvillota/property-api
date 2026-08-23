namespace Property.Application.Common.Interfaces;

/// <summary>Enqueues an outbox message on the current DbContext (no SaveChanges).</summary>
public interface IOutboxWriter
{
    Task EnqueueAsync(
        string messageType,
        object payload,
        CancellationToken cancellationToken = default);
}
