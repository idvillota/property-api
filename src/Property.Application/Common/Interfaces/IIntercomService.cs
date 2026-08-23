using Property.Application.Features.Reception.Visits;

namespace Property.Application.Common.Interfaces;

public interface IIntercomService
{
    /// <summary>
    /// Creates in-app notifications for unit occupants, broadcasts via SignalR,
    /// and always enqueues email fallback (REC-012).
    /// </summary>
    Task NotifyUnitAsync(Guid visitId, CancellationToken cancellationToken = default);
}

/// <summary>Realtime fan-out for visit intercom (SignalR). Tests may use a no-op.</summary>
public interface IVisitIntercomRealtime
{
    Task BroadcastToPersonAsync(
        Guid tenantId,
        Guid personId,
        VisitIntercomAlertDto alert,
        CancellationToken cancellationToken = default);

    /// <summary>Connection count for the person group; 0 means offline for fallback tests.</summary>
    Task<int> GetPersonConnectionCountAsync(
        Guid tenantId,
        Guid personId,
        CancellationToken cancellationToken = default);
}

public sealed class NullVisitIntercomRealtime : IVisitIntercomRealtime
{
    public Task BroadcastToPersonAsync(
        Guid tenantId,
        Guid personId,
        VisitIntercomAlertDto alert,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<int> GetPersonConnectionCountAsync(
        Guid tenantId,
        Guid personId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(0);
}
