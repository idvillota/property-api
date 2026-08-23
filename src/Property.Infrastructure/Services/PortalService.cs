using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Organization.UserPersonLinks;
using Property.Application.Features.Portal;
using Property.Domain.Enums;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class PortalService : IPortalService
{
    private readonly ApplicationDbContext _db;
    private readonly IUserPersonLinkService _links;

    public PortalService(ApplicationDbContext db, IUserPersonLinkService links)
    {
        _db = db;
        _links = links;
    }

    public Task<PortalMeDto> GetMeAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _links.GetPortalMeAsync(userId, cancellationToken);

    public async Task<PortalHomeDto> GetHomeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var me = await _links.GetPortalMeAsync(userId, cancellationToken);

        var occupancies = await _db.UnitOccupancies.AsNoTracking()
            .Where(o => o.PersonId == me.PersonId && o.IsActive)
            .Include(o => o.Unit)
            .OrderByDescending(o => o.IsPrimary)
            .ThenBy(o => o.Unit!.Code)
            .ToListAsync(cancellationToken);

        var allowed = me.UnitIds.ToHashSet();
        var units = occupancies
            .Where(o => allowed.Contains(o.UnitId) && o.Unit is not null)
            .GroupBy(o => o.UnitId)
            .Select(g =>
            {
                var primary = g.OrderByDescending(x => x.IsPrimary).First();
                return new PortalUnitCardDto
                {
                    UnitId = primary.UnitId,
                    Code = primary.Unit!.Code,
                    OccupancyRole = primary.Role,
                    IsPrimary = g.Any(x => x.IsPrimary),
                };
            })
            .ToList();

        var propertyName = await _db.HorizontalProperties.AsNoTracking()
            .Select(p => p.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return new PortalHomeDto
        {
            PersonId = me.PersonId,
            PersonFullName = me.PersonFullName,
            PropertyName = propertyName,
            Units = units,
            QuickLinks =
            [
                new PortalQuickLinkDto { Id = "cuenta", Label = "Cuenta", Path = "/portal/cuenta", Available = false },
                new PortalQuickLinkDto { Id = "reservas", Label = "Reservas", Path = "/portal/reservas", Available = false },
                new PortalQuickLinkDto { Id = "mural", Label = "Mural", Path = "/portal/mural", Available = false },
                new PortalQuickLinkDto { Id = "pqrs", Label = "PQRS", Path = "/portal/pqrs", Available = false },
            ],
        };
    }

    public async Task<bool> IsUnitInScopeAsync(
        Guid userId,
        Guid unitId,
        CancellationToken cancellationToken = default)
    {
        var (_, unitIds) = await _links.GetPortalScopeAsync(userId, cancellationToken);
        return unitIds.Contains(unitId);
    }
}
