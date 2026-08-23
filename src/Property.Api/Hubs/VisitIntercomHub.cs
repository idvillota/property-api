using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Property.Application.Features.Reception.Visits;

namespace Property.Api.Hubs;

[Authorize]
public sealed class VisitIntercomHub : Hub
{
    public static string PersonGroup(Guid tenantId, Guid personId) =>
        $"tenant:{tenantId}:person:{personId}";

    public Task JoinPersonGroup(Guid tenantId, Guid personId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, PersonGroup(tenantId, personId));
}

public sealed class SignalRVisitIntercomRealtime : Application.Common.Interfaces.IVisitIntercomRealtime
{
    private readonly IHubContext<VisitIntercomHub> _hub;

    public SignalRVisitIntercomRealtime(IHubContext<VisitIntercomHub> hub) => _hub = hub;

    public Task BroadcastToPersonAsync(
        Guid tenantId,
        Guid personId,
        VisitIntercomAlertDto alert,
        CancellationToken cancellationToken = default) =>
        _hub.Clients
            .Group(VisitIntercomHub.PersonGroup(tenantId, personId))
            .SendAsync("VisitAlert", alert, cancellationToken);

    public Task<int> GetPersonConnectionCountAsync(
        Guid tenantId,
        Guid personId,
        CancellationToken cancellationToken = default) =>
        // HubContext does not expose group membership counts; callers treat unknown as offline.
        Task.FromResult(0);
}
