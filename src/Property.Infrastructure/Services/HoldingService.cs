using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Holding;
using Property.Domain.Enums;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class HoldingService : IHoldingService
{
    private readonly ApplicationDbContext _db;

    public HoldingService(ApplicationDbContext db) => _db = db;

    public async Task<HoldingSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var properties = await _db.HorizontalProperties.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name })
            .ToListAsync(cancellationToken);

        if (properties.Count == 0)
            return new HoldingSummaryDto();

        var propertyIds = properties.Select(p => p.Id).ToList();

        var unitCounts = await _db.Units.AsNoTracking()
            .Where(u => propertyIds.Contains(u.HorizontalPropertyId) && u.IsActive)
            .GroupBy(u => u.HorizontalPropertyId)
            .Select(g => new { PropertyId = g.Key, TotalUnits = g.Count() })
            .ToDictionaryAsync(x => x.PropertyId, x => x.TotalUnits, cancellationToken);

        var occupiedUnits = await (
            from o in _db.UnitOccupancies.AsNoTracking()
            join u in _db.Units.AsNoTracking() on o.UnitId equals u.Id
            where propertyIds.Contains(u.HorizontalPropertyId) &&
                  u.IsActive &&
                  o.IsActive &&
                  (o.EndDate == null || o.EndDate >= today)
            select new { u.HorizontalPropertyId, u.Id }
        ).Distinct().ToListAsync(cancellationToken);

        var occupiedByProperty = occupiedUnits
            .GroupBy(x => x.HorizontalPropertyId)
            .ToDictionary(g => g.Key, g => g.Count());

        var openCharges = await _db.FeeCharges.AsNoTracking()
            .Where(c =>
                propertyIds.Contains(c.HorizontalPropertyId) &&
                c.IsActive &&
                (c.Status == FeeChargeStatus.Pending || c.Status == FeeChargeStatus.Partial) &&
                c.Amount > c.AmountPaid)
            .Select(c => new
            {
                c.HorizontalPropertyId,
                Balance = c.Amount - c.AmountPaid,
                c.DueDate,
            })
            .ToListAsync(cancellationToken);

        var balancesByProperty = openCharges
            .GroupBy(c => c.HorizontalPropertyId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    OpenBalance = Math.Round(g.Sum(x => x.Balance), 2, MidpointRounding.AwayFromZero),
                    OverdueBalance = Math.Round(
                        g.Where(x => x.DueDate < today).Sum(x => x.Balance),
                        2,
                        MidpointRounding.AwayFromZero),
                });

        var summaries = properties.Select(p =>
        {
            balancesByProperty.TryGetValue(p.Id, out var balances);
            return new HoldingPropertySummaryDto
            {
                PropertyId = p.Id,
                Name = p.Name,
                OpenBalance = balances?.OpenBalance ?? 0m,
                OverdueBalance = balances?.OverdueBalance ?? 0m,
                OccupiedUnits = occupiedByProperty.GetValueOrDefault(p.Id),
                TotalUnits = unitCounts.GetValueOrDefault(p.Id),
            };
        }).ToList();

        return new HoldingSummaryDto { Properties = summaries };
    }
}
