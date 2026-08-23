using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Amenities.Reservations;
using Property.Application.Features.Organization.UserPersonLinks;
using Property.Application.Features.Portal;
using Property.Domain.Enums;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class PortalService : IPortalService
{
    private readonly ApplicationDbContext _db;
    private readonly IUserPersonLinkService _links;
    private readonly IReservationService _reservations;

    public PortalService(
        ApplicationDbContext db,
        IUserPersonLinkService links,
        IReservationService reservations)
    {
        _db = db;
        _links = links;
        _reservations = reservations;
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
                new PortalQuickLinkDto { Id = "reservas", Label = "Reservas", Path = "/portal/reservas", Available = true },
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

    public async Task<IReadOnlyList<PortalCommonAreaDto>> ListReservationAreasAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        _ = await _links.GetPortalMeAsync(userId, cancellationToken);

        return await _db.CommonAreas.AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.Name)
            .Select(a => new PortalCommonAreaDto
            {
                Id = a.Id,
                HorizontalPropertyId = a.HorizontalPropertyId,
                Name = a.Name,
                AreaType = a.AreaType,
                Capacity = a.Capacity,
                RequiresDeposit = a.RequiresDeposit,
                DepositAmount = a.DepositAmount,
                RulesNotes = a.RulesNotes,
                MinDurationMinutes = a.MinDurationMinutes,
                MaxDurationMinutes = a.MaxDurationMinutes,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PortalReservationSlotDto>> ListOccupiedSlotsAsync(
        Guid userId,
        Guid commonAreaId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        _ = await _links.GetPortalMeAsync(userId, cancellationToken);

        if (toUtc <= fromUtc)
            throw new InvalidOperationException("El rango de fechas no es válido.");

        var areaExists = await _db.CommonAreas.AsNoTracking()
            .AnyAsync(a => a.Id == commonAreaId && a.IsActive, cancellationToken);
        if (!areaExists)
            throw new InvalidOperationException("No encontramos la zona común seleccionada.");

        return await _db.Reservations.AsNoTracking()
            .Where(r =>
                r.IsActive &&
                r.CommonAreaId == commonAreaId &&
                (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed) &&
                r.StartsAtUtc < toUtc &&
                r.EndsAtUtc > fromUtc)
            .OrderBy(r => r.StartsAtUtc)
            .Select(r => new PortalReservationSlotDto
            {
                StartsAtUtc = r.StartsAtUtc,
                EndsAtUtc = r.EndsAtUtc,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PortalReservationDto>> ListMyReservationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var me = await _links.GetPortalMeAsync(userId, cancellationToken);
        var now = DateTime.UtcNow;

        var rows = await (
            from r in _db.Reservations.AsNoTracking()
            join a in _db.CommonAreas.AsNoTracking() on r.CommonAreaId equals a.Id
            join u in _db.Units.AsNoTracking() on r.UnitId equals u.Id
            where r.PersonId == me.PersonId && r.IsActive
            orderby r.StartsAtUtc descending
            select new { Reservation = r, AreaName = a.Name, UnitCode = u.Code }
        ).Take(50).ToListAsync(cancellationToken);

        return rows.Select(x => MapPortalReservation(x.Reservation, x.AreaName, x.UnitCode, now)).ToList();
    }

    public async Task<PortalReservationDto> CreateReservationAsync(
        Guid userId,
        CreatePortalReservationDto dto,
        CancellationToken cancellationToken = default)
    {
        var me = await _links.GetPortalMeAsync(userId, cancellationToken);

        if (!await IsUnitInScopeAsync(userId, dto.UnitId, cancellationToken))
            throw new UnauthorizedAccessException("Unit is not in portal scope.");

        var area = await _db.CommonAreas.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == dto.CommonAreaId && a.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("No encontramos la zona común seleccionada.");

        ValidateDuration(area.MinDurationMinutes, area.MaxDurationMinutes, dto.StartsAtUtc, dto.EndsAtUtc);

        var created = await _reservations.CreateAsync(
            new CreateReservationDto
            {
                HorizontalPropertyId = area.HorizontalPropertyId,
                CommonAreaId = dto.CommonAreaId,
                UnitId = dto.UnitId,
                PersonId = me.PersonId,
                StartsAtUtc = dto.StartsAtUtc,
                EndsAtUtc = dto.EndsAtUtc,
                Status = ReservationStatus.Confirmed,
                AttendeesCount = dto.AttendeesCount,
                Notes = dto.Notes,
                DepositPaid = false,
            },
            cancellationToken);

        var unitCode = await _db.Units.AsNoTracking()
            .Where(u => u.Id == created.UnitId)
            .Select(u => u.Code)
            .FirstAsync(cancellationToken);

        return new PortalReservationDto
        {
            Id = created.Id,
            CommonAreaId = created.CommonAreaId,
            CommonAreaName = area.Name,
            UnitId = created.UnitId,
            UnitCode = unitCode,
            StartsAtUtc = created.StartsAtUtc,
            EndsAtUtc = created.EndsAtUtc,
            Status = created.Status,
            AttendeesCount = created.AttendeesCount,
            Notes = created.Notes,
            IsActive = created.IsActive,
            CanCancel = created.IsActive && created.StartsAtUtc > DateTime.UtcNow,
        };
    }

    public async Task CancelReservationAsync(
        Guid userId,
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        var me = await _links.GetPortalMeAsync(userId, cancellationToken);

        var entity = await _db.Reservations
            .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken)
            ?? throw new InvalidOperationException("No encontramos la reserva.");

        if (entity.PersonId != me.PersonId)
            throw new UnauthorizedAccessException("Solo puedes cancelar tus propias reservas.");

        if (!entity.IsActive)
            return;

        if (entity.StartsAtUtc <= DateTime.UtcNow)
            throw new InvalidOperationException("Solo puedes cancelar reservas futuras.");

        entity.Status = ReservationStatus.Cancelled;
        entity.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsUnitInScopeAsync(
        Guid userId,
        Guid unitId,
        CancellationToken cancellationToken = default)
    {
        var (_, unitIds) = await _links.GetPortalScopeAsync(userId, cancellationToken);
        return unitIds.Contains(unitId);
    }

    private static void ValidateDuration(
        int? minMinutes,
        int? maxMinutes,
        DateTime startsAtUtc,
        DateTime endsAtUtc)
    {
        if (endsAtUtc <= startsAtUtc)
            throw new InvalidOperationException("La hora de fin debe ser posterior a la hora de inicio.");

        var minutes = (int)Math.Round((endsAtUtc - startsAtUtc).TotalMinutes);
        if (minMinutes is > 0 && minutes < minMinutes.Value)
            throw new InvalidOperationException($"La reserva debe durar al menos {minMinutes} minutos.");
        if (maxMinutes is > 0 && minutes > maxMinutes.Value)
            throw new InvalidOperationException($"La reserva no puede durar más de {maxMinutes} minutos.");
    }

    private static PortalReservationDto MapPortalReservation(
        Domain.Entities.Reservation entity,
        string areaName,
        string unitCode,
        DateTime nowUtc) =>
        new()
        {
            Id = entity.Id,
            CommonAreaId = entity.CommonAreaId,
            CommonAreaName = areaName,
            UnitId = entity.UnitId,
            UnitCode = unitCode,
            StartsAtUtc = entity.StartsAtUtc,
            EndsAtUtc = entity.EndsAtUtc,
            Status = entity.Status,
            AttendeesCount = entity.AttendeesCount,
            Notes = entity.Notes,
            IsActive = entity.IsActive,
            CanCancel = entity.IsActive && entity.StartsAtUtc > nowUtc,
        };
}
