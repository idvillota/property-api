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
                new PortalQuickLinkDto { Id = "cuenta", Label = "Cuenta", Path = "/portal/cuenta", Available = true },
                new PortalQuickLinkDto { Id = "reservas", Label = "Reservas", Path = "/portal/reservas", Available = false },
                new PortalQuickLinkDto { Id = "mural", Label = "Mural", Path = "/portal/mural", Available = false },
                new PortalQuickLinkDto { Id = "pqrs", Label = "PQRS", Path = "/portal/pqrs", Available = false },
            ],
        };
    }

    public async Task<PortalAccountDto> GetAccountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var me = await _links.GetPortalMeAsync(userId, cancellationToken);
        var unitIds = me.UnitIds.ToList();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var unitRows = unitIds.Count == 0
            ? []
            : await _db.Units.AsNoTracking()
                .Where(u => unitIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Code })
                .OrderBy(u => u.Code)
                .ToListAsync(cancellationToken);

        var openCharges = unitIds.Count == 0
            ? []
            : await _db.FeeCharges.AsNoTracking()
                .Where(c =>
                    unitIds.Contains(c.UnitId) &&
                    c.IsActive &&
                    (c.Status == FeeChargeStatus.Pending || c.Status == FeeChargeStatus.Partial))
                .Select(c => new { c.UnitId, Balance = c.Amount - c.AmountPaid, c.DueDate })
                .ToListAsync(cancellationToken);

        var units = unitRows.Select(u =>
        {
            var unitCharges = openCharges.Where(c => c.UnitId == u.Id).ToList();
            return new PortalAccountUnitDto
            {
                UnitId = u.Id,
                Code = u.Code,
                OpenBalance = unitCharges.Sum(c => c.Balance),
                OverdueBalance = unitCharges.Where(c => c.DueDate < today).Sum(c => c.Balance),
                OpenChargeCount = unitCharges.Count,
            };
        }).ToList();

        var recentPayments = unitIds.Count == 0
            ? []
            : await (
                from pay in _db.Payments.AsNoTracking()
                join u in _db.Units.AsNoTracking() on pay.UnitId equals u.Id
                where pay.IsActive && unitIds.Contains(pay.UnitId)
                orderby pay.PaidAt descending
                select new PortalAccountPaymentDto
                {
                    PaymentId = pay.Id,
                    UnitId = pay.UnitId,
                    UnitCode = u.Code,
                    Amount = pay.Amount,
                    PaidAt = pay.PaidAt,
                    Method = pay.Method,
                }
            ).Take(20).ToListAsync(cancellationToken);

        return new PortalAccountDto
        {
            PersonId = me.PersonId,
            PersonFullName = me.PersonFullName,
            OpenBalance = units.Sum(u => u.OpenBalance),
            OverdueBalance = units.Sum(u => u.OverdueBalance),
            OpenChargeCount = openCharges.Count,
            Units = units,
            RecentPayments = recentPayments,
        };
    }

    public async Task<PortalAccountUnitDetailDto> GetAccountUnitAsync(
        Guid userId,
        Guid unitId,
        CancellationToken cancellationToken = default)
    {
        _ = await _links.GetPortalMeAsync(userId, cancellationToken);

        if (!await IsUnitInScopeAsync(userId, unitId, cancellationToken))
            throw new UnauthorizedAccessException("Unit is not in portal scope.");

        var unit = await _db.Units.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == unitId, cancellationToken)
            ?? throw new InvalidOperationException("Unit was not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var openCharges = await (
            from c in _db.FeeCharges.AsNoTracking()
            join p in _db.BillingPeriods.AsNoTracking() on c.BillingPeriodId equals p.Id into pj
            from p in pj.DefaultIfEmpty()
            where c.UnitId == unitId &&
                  c.IsActive &&
                  (c.Status == FeeChargeStatus.Pending || c.Status == FeeChargeStatus.Partial)
            orderby c.DueDate
            select new PortalAccountChargeDto
            {
                FeeChargeId = c.Id,
                Description = c.Description,
                PeriodKey = p != null ? $"{p.Year:D4}-{p.Month:D2}" : null,
                ChargeType = c.ChargeType,
                Amount = c.Amount,
                AmountPaid = c.AmountPaid,
                Balance = c.Amount - c.AmountPaid,
                DueDate = c.DueDate,
                Status = c.Status,
            }
        ).ToListAsync(cancellationToken);

        var recentPayments = await (
            from pay in _db.Payments.AsNoTracking()
            where pay.IsActive && pay.UnitId == unitId
            orderby pay.PaidAt descending
            select new PortalAccountPaymentDto
            {
                PaymentId = pay.Id,
                UnitId = pay.UnitId,
                UnitCode = unit.Code,
                Amount = pay.Amount,
                PaidAt = pay.PaidAt,
                Method = pay.Method,
            }
        ).Take(20).ToListAsync(cancellationToken);

        return new PortalAccountUnitDetailDto
        {
            UnitId = unit.Id,
            Code = unit.Code,
            OpenBalance = openCharges.Sum(c => c.Balance),
            OverdueBalance = openCharges.Where(c => c.DueDate < today).Sum(c => c.Balance),
            OpenCharges = openCharges,
            RecentPayments = recentPayments,
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
