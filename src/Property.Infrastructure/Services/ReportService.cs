using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Reports;
using Property.Domain.Common;
using Property.Domain.Enums;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;

    public ReportService(ApplicationDbContext db) => _db = db;

    public async Task<ReportOverviewDto> GetOverviewAsync(
        Guid horizontalPropertyId,
        CancellationToken cancellationToken = default)
    {
        await EnsurePropertyExistsAsync(horizontalPropertyId, cancellationToken);

        var today = TodayUtc();
        var nowUtc = DateTime.UtcNow;
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var (monthFromUtc, monthToExclusiveUtc) = ToUtcRange(monthStart, today);
        var upcomingEndUtc = nowUtc.AddDays(7);

        var units = _db.Units.AsNoTracking()
            .Where(u => u.HorizontalPropertyId == horizontalPropertyId && u.IsActive);

        var unitCount = await units.CountAsync(cancellationToken);
        var billableUnitCount = await units.CountAsync(u => u.IsBillable, cancellationToken);
        var totalCoefficient = await units.SumAsync(u => (decimal?)u.Coefficient, cancellationToken) ?? 0m;

        var activeOccupancyCount = await _db.UnitOccupancies.AsNoTracking()
            .Where(o =>
                o.IsActive &&
                (o.EndDate == null || o.EndDate >= today) &&
                _db.Units.Any(u =>
                    u.Id == o.UnitId &&
                    u.HorizontalPropertyId == horizontalPropertyId &&
                    u.IsActive))
            .CountAsync(cancellationToken);

        var openCharges = await _db.FeeCharges.AsNoTracking()
            .Where(c =>
                c.HorizontalPropertyId == horizontalPropertyId &&
                c.IsActive &&
                (c.Status == FeeChargeStatus.Pending || c.Status == FeeChargeStatus.Partial) &&
                c.Amount > c.AmountPaid)
            .Select(c => new { c.Amount, c.AmountPaid, c.DueDate })
            .ToListAsync(cancellationToken);

        var openChargesAmount = RoundMoney(openCharges.Sum(c => c.Amount - c.AmountPaid));
        var overdueChargesAmount = RoundMoney(
            openCharges.Where(c => c.DueDate < today).Sum(c => c.Amount - c.AmountPaid));

        var collectedAmountThisMonth = RoundMoney(
            await _db.Payments.AsNoTracking()
                .Where(p =>
                    p.HorizontalPropertyId == horizontalPropertyId &&
                    p.IsActive &&
                    p.PaidAt >= monthFromUtc &&
                    p.PaidAt < monthToExclusiveUtc)
                .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m);

        var pendingReceptionCount = await _db.ReceptionItems.AsNoTracking()
            .CountAsync(
                r => r.HorizontalPropertyId == horizontalPropertyId &&
                     r.IsActive &&
                     r.Status == ReceptionItemStatus.PendingPickup,
                cancellationToken);

        var upcomingReservationsCount = await _db.Reservations.AsNoTracking()
            .CountAsync(
                r => r.HorizontalPropertyId == horizontalPropertyId &&
                     r.IsActive &&
                     (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Pending) &&
                     r.StartsAtUtc >= nowUtc &&
                     r.StartsAtUtc < upcomingEndUtc,
                cancellationToken);

        return new ReportOverviewDto
        {
            UnitCount = unitCount,
            BillableUnitCount = billableUnitCount,
            ActiveOccupancyCount = activeOccupancyCount,
            TotalCoefficient = RoundCoefficient(totalCoefficient),
            RemainingCoefficientToHundred = RoundCoefficient(CoefficientRules.Total - totalCoefficient),
            OpenChargesAmount = openChargesAmount,
            OverdueChargesAmount = overdueChargesAmount,
            CollectedAmountThisMonth = collectedAmountThisMonth,
            PendingReceptionCount = pendingReceptionCount,
            UpcomingReservationsCount = upcomingReservationsCount,
        };
    }

    public async Task<CarteraReportDto> GetCarteraAsync(
        Guid horizontalPropertyId,
        DateOnly? asOfDate,
        CancellationToken cancellationToken = default)
    {
        await EnsurePropertyExistsAsync(horizontalPropertyId, cancellationToken);

        var asOf = asOfDate ?? TodayUtc();

        var charges = await _db.FeeCharges.AsNoTracking()
            .Where(c =>
                c.HorizontalPropertyId == horizontalPropertyId &&
                c.IsActive &&
                (c.Status == FeeChargeStatus.Pending || c.Status == FeeChargeStatus.Partial) &&
                c.DueDate <= asOf &&
                c.Amount > c.AmountPaid)
            .Select(c => new
            {
                c.UnitId,
                UnitCode = c.Unit!.Code,
                UnitType = c.Unit.UnitType,
                Balance = c.Amount - c.AmountPaid,
                c.DueDate,
            })
            .ToListAsync(cancellationToken);

        var items = charges
            .GroupBy(c => new { c.UnitId, c.UnitCode, c.UnitType })
            .Select(g =>
            {
                var balance = RoundMoney(g.Sum(x => x.Balance));
                var overdueBalance = RoundMoney(g.Where(x => x.DueDate < asOf).Sum(x => x.Balance));
                return new CarteraItemDto
                {
                    UnitId = g.Key.UnitId,
                    UnitCode = g.Key.UnitCode,
                    UnitType = g.Key.UnitType,
                    Balance = balance,
                    OverdueBalance = overdueBalance,
                    OldestDueDate = g.Min(x => x.DueDate),
                    ChargeCount = g.Count(),
                };
            })
            .Where(i => i.Balance > 0m)
            .OrderByDescending(i => i.Balance)
            .ThenBy(i => i.UnitCode)
            .ToList();

        return new CarteraReportDto
        {
            AsOfDate = asOf,
            Totals = new CarteraTotalsDto
            {
                UnitCount = items.Count,
                TotalBalance = RoundMoney(items.Sum(i => i.Balance)),
                TotalOverdue = RoundMoney(items.Sum(i => i.OverdueBalance)),
            },
            Items = items,
        };
    }

    public async Task<CollectionsReportDto> GetCollectionsAsync(
        Guid horizontalPropertyId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        await EnsurePropertyExistsAsync(horizontalPropertyId, cancellationToken);

        var today = TodayUtc();
        var fromDate = from ?? new DateOnly(today.Year, today.Month, 1);
        var toDate = to ?? today;
        EnsureValidRange(fromDate, toDate);

        var (fromUtc, toExclusiveUtc) = ToUtcRange(fromDate, toDate);

        var payments = await _db.Payments.AsNoTracking()
            .Where(p =>
                p.HorizontalPropertyId == horizontalPropertyId &&
                p.IsActive &&
                p.PaidAt >= fromUtc &&
                p.PaidAt < toExclusiveUtc)
            .Select(p => new
            {
                p.Id,
                p.UnitId,
                UnitCode = p.Unit!.Code,
                p.Amount,
                p.PaidAt,
                p.Method,
                p.Reference,
            })
            .ToListAsync(cancellationToken);

        var byMethod = payments
            .GroupBy(p => p.Method)
            .Select(g => new CollectionsByMethodDto
            {
                Method = g.Key,
                Count = g.Count(),
                Amount = RoundMoney(g.Sum(x => x.Amount)),
            })
            .OrderBy(x => x.Method)
            .ToList();

        var byDay = payments
            .GroupBy(p => DateOnly.FromDateTime(p.PaidAt))
            .Select(g => new CollectionsByDayDto
            {
                Date = g.Key,
                Count = g.Count(),
                Amount = RoundMoney(g.Sum(x => x.Amount)),
            })
            .OrderBy(x => x.Date)
            .ToList();

        var items = payments
            .OrderByDescending(p => p.PaidAt)
            .ThenByDescending(p => p.Id)
            .Take(100)
            .Select(p => new CollectionsItemDto
            {
                PaymentId = p.Id,
                UnitId = p.UnitId,
                UnitCode = p.UnitCode,
                Amount = RoundMoney(p.Amount),
                PaidAt = p.PaidAt,
                Method = p.Method,
                Reference = p.Reference,
            })
            .ToList();

        return new CollectionsReportDto
        {
            From = fromDate,
            To = toDate,
            Totals = new CollectionsTotalsDto
            {
                PaymentCount = payments.Count,
                TotalAmount = RoundMoney(payments.Sum(p => p.Amount)),
            },
            ByMethod = byMethod,
            ByDay = byDay,
            Items = items,
        };
    }

    public async Task<OccupancyReportDto> GetOccupancyAsync(
        Guid horizontalPropertyId,
        CancellationToken cancellationToken = default)
    {
        await EnsurePropertyExistsAsync(horizontalPropertyId, cancellationToken);

        var today = TodayUtc();

        var units = await _db.Units.AsNoTracking()
            .Where(u => u.HorizontalPropertyId == horizontalPropertyId && u.IsActive)
            .Select(u => new { u.Id, u.Code, u.UnitType, u.IsBillable })
            .OrderBy(u => u.Code)
            .ToListAsync(cancellationToken);

        var unitIds = units.Select(u => u.Id).ToList();

        var occupancies = await _db.UnitOccupancies.AsNoTracking()
            .Where(o =>
                unitIds.Contains(o.UnitId) &&
                o.IsActive &&
                (o.EndDate == null || o.EndDate >= today))
            .Select(o => new
            {
                o.UnitId,
                o.Role,
                o.IsPrimary,
                o.ReceivesBilling,
                PersonName = o.Person!.FirstName + " " + o.Person.LastName,
            })
            .ToListAsync(cancellationToken);

        var byUnit = occupancies.ToLookup(o => o.UnitId);

        var items = units.Select(u =>
        {
            var unitOccupancies = byUnit[u.Id].ToList();
            var owner = unitOccupancies
                .Where(o => o.Role == OccupancyRole.Owner)
                .OrderByDescending(o => o.IsPrimary)
                .ThenBy(o => o.PersonName)
                .FirstOrDefault();
            var resident = unitOccupancies
                .Where(o => o.Role is OccupancyRole.Resident or OccupancyRole.Tenant)
                .OrderByDescending(o => o.IsPrimary)
                .ThenBy(o => o.PersonName)
                .FirstOrDefault();

            return new OccupancyItemDto
            {
                UnitId = u.Id,
                UnitCode = u.Code,
                UnitType = u.UnitType,
                IsBillable = u.IsBillable,
                OwnerName = owner?.PersonName,
                ResidentName = resident?.PersonName,
                OccupancyCount = unitOccupancies.Count,
                HasBillingContact = unitOccupancies.Any(o => o.ReceivesBilling),
            };
        }).ToList();

        return new OccupancyReportDto
        {
            Totals = new OccupancyTotalsDto
            {
                UnitCount = items.Count,
                UnitsWithOwner = items.Count(i => i.OwnerName is not null),
                UnitsWithResident = items.Count(i => i.ResidentName is not null),
                UnitsVacant = items.Count(i => i.OccupancyCount == 0),
            },
            Items = items,
        };
    }

    public async Task<CoefficientsReportDto> GetCoefficientsAsync(
        Guid horizontalPropertyId,
        CancellationToken cancellationToken = default)
    {
        await EnsurePropertyExistsAsync(horizontalPropertyId, cancellationToken);

        var units = await _db.Units.AsNoTracking()
            .Where(u => u.HorizontalPropertyId == horizontalPropertyId)
            .Select(u => new CoefficientItemDto
            {
                UnitId = u.Id,
                UnitCode = u.Code,
                UnitType = u.UnitType,
                Coefficient = u.Coefficient,
                PrivateBuiltAreaM2 = u.PrivateBuiltAreaM2,
                IsBillable = u.IsBillable,
                IsActive = u.IsActive,
            })
            .ToListAsync(cancellationToken);

        var items = units
            .OrderByDescending(u => u.Coefficient)
            .ThenBy(u => u.UnitCode)
            .ToList();

        var activeItems = items.Where(u => u.IsActive).ToList();
        var totalCoefficient = activeItems.Sum(u => u.Coefficient);

        return new CoefficientsReportDto
        {
            Totals = new CoefficientsTotalsDto
            {
                UnitCount = activeItems.Count,
                BillableUnitCount = activeItems.Count(u => u.IsBillable),
                TotalCoefficient = RoundCoefficient(totalCoefficient),
                RemainingToHundred = RoundCoefficient(CoefficientRules.Total - totalCoefficient),
            },
            Items = items,
        };
    }

    public async Task<ReservationUsageReportDto> GetReservationUsageAsync(
        Guid horizontalPropertyId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        await EnsurePropertyExistsAsync(horizontalPropertyId, cancellationToken);

        var today = TodayUtc();
        var fromDate = from ?? today.AddDays(-30);
        var toDate = to ?? today;
        EnsureValidRange(fromDate, toDate);

        var (fromUtc, toExclusiveUtc) = ToUtcRange(fromDate, toDate);

        var reservations = await _db.Reservations.AsNoTracking()
            .Where(r =>
                r.HorizontalPropertyId == horizontalPropertyId &&
                r.IsActive &&
                r.StartsAtUtc >= fromUtc &&
                r.StartsAtUtc < toExclusiveUtc)
            .Select(r => new
            {
                r.Id,
                r.CommonAreaId,
                CommonAreaName = r.CommonArea!.Name,
                UnitCode = r.Unit!.Code,
                r.StartsAtUtc,
                r.EndsAtUtc,
                r.Status,
                r.AttendeesCount,
            })
            .ToListAsync(cancellationToken);

        var byArea = reservations
            .GroupBy(r => new { r.CommonAreaId, r.CommonAreaName })
            .Select(g => new ReservationUsageByAreaDto
            {
                CommonAreaId = g.Key.CommonAreaId,
                CommonAreaName = g.Key.CommonAreaName,
                Count = g.Count(),
                ConfirmedCount = g.Count(x => x.Status == ReservationStatus.Confirmed),
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.CommonAreaName)
            .ToList();

        var items = reservations
            .OrderByDescending(r => r.StartsAtUtc)
            .ThenByDescending(r => r.Id)
            .Take(100)
            .Select(r => new ReservationUsageItemDto
            {
                ReservationId = r.Id,
                CommonAreaName = r.CommonAreaName,
                UnitCode = r.UnitCode,
                StartsAtUtc = r.StartsAtUtc,
                EndsAtUtc = r.EndsAtUtc,
                Status = r.Status,
                AttendeesCount = r.AttendeesCount,
            })
            .ToList();

        return new ReservationUsageReportDto
        {
            From = fromDate,
            To = toDate,
            Totals = new ReservationUsageTotalsDto
            {
                ReservationCount = reservations.Count,
                ConfirmedCount = reservations.Count(r => r.Status == ReservationStatus.Confirmed),
                CancelledCount = reservations.Count(r => r.Status == ReservationStatus.Cancelled),
                CompletedCount = reservations.Count(r => r.Status == ReservationStatus.Completed),
            },
            ByArea = byArea,
            Items = items,
        };
    }

    public async Task<ReceptionReportDto> GetReceptionAsync(
        Guid horizontalPropertyId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        await EnsurePropertyExistsAsync(horizontalPropertyId, cancellationToken);

        var today = TodayUtc();
        var fromDate = from ?? today.AddDays(-30);
        var toDate = to ?? today;
        EnsureValidRange(fromDate, toDate);

        var (fromUtc, toExclusiveUtc) = ToUtcRange(fromDate, toDate);

        var itemsRaw = await _db.ReceptionItems.AsNoTracking()
            .Where(r =>
                r.HorizontalPropertyId == horizontalPropertyId &&
                r.IsActive &&
                r.ReceivedAtUtc >= fromUtc &&
                r.ReceivedAtUtc < toExclusiveUtc)
            .Select(r => new
            {
                r.Id,
                r.ItemType,
                UnitCode = r.Unit != null ? r.Unit.Code : null,
                r.RecipientName,
                r.Status,
                r.ReceivedAtUtc,
                r.DeliveredAtUtc,
            })
            .ToListAsync(cancellationToken);

        var byType = itemsRaw
            .GroupBy(r => r.ItemType)
            .Select(g => new ReceptionReportByTypeDto
            {
                ItemType = g.Key,
                Count = g.Count(),
            })
            .OrderBy(x => x.ItemType)
            .ToList();

        var items = itemsRaw
            .OrderByDescending(r => r.ReceivedAtUtc)
            .ThenByDescending(r => r.Id)
            .Take(100)
            .Select(r => new ReceptionReportItemDto
            {
                Id = r.Id,
                ItemType = r.ItemType,
                UnitCode = r.UnitCode,
                RecipientName = r.RecipientName,
                Status = r.Status,
                ReceivedAtUtc = r.ReceivedAtUtc,
                DeliveredAtUtc = r.DeliveredAtUtc,
            })
            .ToList();

        return new ReceptionReportDto
        {
            From = fromDate,
            To = toDate,
            Totals = new ReceptionReportTotalsDto
            {
                Total = itemsRaw.Count,
                PendingPickup = itemsRaw.Count(r => r.Status == ReceptionItemStatus.PendingPickup),
                Delivered = itemsRaw.Count(r => r.Status == ReceptionItemStatus.Delivered),
                Returned = itemsRaw.Count(r => r.Status == ReceptionItemStatus.Returned),
            },
            ByType = byType,
            Items = items,
        };
    }

    private async Task EnsurePropertyExistsAsync(Guid horizontalPropertyId, CancellationToken cancellationToken)
    {
        var exists = await _db.HorizontalProperties.AsNoTracking()
            .AnyAsync(p => p.Id == horizontalPropertyId, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("Horizontal property was not found.");
    }

    private static void EnsureValidRange(DateOnly from, DateOnly to)
    {
        if (from > to)
            throw new InvalidOperationException("'from' must be on or before 'to'.");
    }

    private static DateOnly TodayUtc() => DateOnly.FromDateTime(DateTime.UtcNow);

    private static (DateTime FromUtc, DateTime ToExclusiveUtc) ToUtcRange(DateOnly from, DateOnly to)
    {
        var fromUtc = DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var toExclusiveUtc = DateTime.SpecifyKind(to.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        return (fromUtc, toExclusiveUtc);
    }

    private static decimal RoundMoney(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static decimal RoundCoefficient(decimal value) =>
        Math.Round(value, CoefficientRules.DecimalPlaces, MidpointRounding.AwayFromZero);
}
