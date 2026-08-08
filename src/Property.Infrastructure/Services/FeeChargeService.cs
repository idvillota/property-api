using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Billing.FeeCharges;
using Property.Domain.Common;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Common;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class FeeChargeService : IFeeChargeService
{
    private readonly ApplicationDbContext _db;
    private readonly IRepository<FeeCharge> _charges;
    private readonly IRepository<BillingPeriod> _periods;
    private readonly IRepository<Unit> _units;
    private readonly IRepository<HorizontalProperty> _properties;
    private readonly IUnitOfWork _unitOfWork;

    public FeeChargeService(
        ApplicationDbContext db,
        IRepository<FeeCharge> charges,
        IRepository<BillingPeriod> periods,
        IRepository<Unit> units,
        IRepository<HorizontalProperty> properties,
        IUnitOfWork unitOfWork)
    {
        _db = db;
        _charges = charges;
        _periods = periods;
        _units = units;
        _properties = properties;
        _unitOfWork = unitOfWork;
    }

    public Task<PagedResult<FeeChargeDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default) =>
        ListQueryHelpers.ToPagedResultAsync(
            _charges.Query().AsNoTracking(),
            query,
            q => PagedEntityQueries.ShapeFeeCharges(q, query),
            entities => Task.FromResult<IReadOnlyList<FeeChargeDto>>(entities.Select(Map).ToList()),
            cancellationToken);

    public async Task<FeeChargeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _charges.Query().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<FeeChargeDto> CreateAsync(CreateFeeChargeDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.ChargeType == FeeChargeType.Ordinary)
            throw new InvalidOperationException("Ordinary charges must be created via generate-ordinary.");

        if (dto.ChargeType == FeeChargeType.Interest)
            throw new InvalidOperationException("Interest charges must be created via generate-interest.");

        await EnsurePropertyUnitPeriodAsync(dto.HorizontalPropertyId, dto.UnitId, dto.BillingPeriodId, cancellationToken);

        var unit = await _units.Query().AsNoTracking().FirstAsync(u => u.Id == dto.UnitId, cancellationToken);
        var entity = new FeeCharge
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = dto.HorizontalPropertyId,
            BillingPeriodId = dto.BillingPeriodId,
            UnitId = dto.UnitId,
            ChargeType = dto.ChargeType,
            Description = dto.Description.Trim(),
            CoefficientSnapshot = unit.Coefficient,
            Amount = Math.Round(dto.Amount, 2, MidpointRounding.AwayFromZero),
            AmountPaid = 0m,
            DueDate = dto.DueDate,
            Status = FeeChargeStatus.Pending,
            IsActive = true,
        };
        await _charges.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<FeeChargeDto?> UpdateAsync(
        Guid id,
        UpdateFeeChargeDto dto,
        CancellationToken cancellationToken = default)
    {
        var entity = await _charges.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        if (entity.Status is FeeChargeStatus.Paid)
            throw new InvalidOperationException("Paid charges cannot be updated.");

        entity.Description = dto.Description.Trim();
        entity.DueDate = dto.DueDate;

        if (dto.Status is FeeChargeStatus.Waived or FeeChargeStatus.Cancelled)
        {
            if (entity.AmountPaid > 0m)
                throw new InvalidOperationException("Charges with payments cannot be waived or cancelled.");
            entity.Status = dto.Status.Value;
        }
        else if (dto.Status.HasValue && dto.Status != entity.Status)
        {
            throw new InvalidOperationException("Status can only be changed to Waived or Cancelled.");
        }

        if (entity.ChargeType == FeeChargeType.Adjustment && dto.Amount.HasValue)
            entity.Amount = Math.Round(dto.Amount.Value, 2, MidpointRounding.AwayFromZero);

        if (dto.IsActive.HasValue)
            entity.IsActive = dto.IsActive.Value;

        _charges.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _charges.GetByIdAsync(id, cancellationToken);
        if (entity is null || !entity.IsActive)
            return false;

        if (entity.AmountPaid > 0m)
            throw new InvalidOperationException("Charges with payments cannot be deactivated.");

        entity.IsActive = false;
        if (entity.Status is FeeChargeStatus.Pending or FeeChargeStatus.Partial)
            entity.Status = FeeChargeStatus.Cancelled;

        _charges.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<GenerateChargesResultDto> GenerateOrdinaryAsync(
        GenerateOrdinaryChargesDto dto,
        CancellationToken cancellationToken = default)
    {
        var period = await _periods.Query()
            .FirstOrDefaultAsync(
                p => p.Id == dto.BillingPeriodId && p.HorizontalPropertyId == dto.HorizontalPropertyId,
                cancellationToken)
            ?? throw new InvalidOperationException("Billing period was not found for the horizontal property.");

        if (period.Status != BillingPeriodStatus.Open)
            throw new InvalidOperationException("Billing period must be Open to generate ordinary charges.");

        var settings = await _db.FeeSettings
            .FirstOrDefaultAsync(s => s.HorizontalPropertyId == dto.HorizontalPropertyId, cancellationToken);
        if (settings is null || settings.MonthlyBudgetAmount <= 0m)
            throw new InvalidOperationException("MonthlyBudgetAmount must be greater than zero.");

        var units = await _units.Query().AsNoTracking()
            .Where(u => u.HorizontalPropertyId == dto.HorizontalPropertyId && u.IsActive && u.IsBillable)
            .ToListAsync(cancellationToken);

        var existingUnitIds = await _charges.Query().AsNoTracking()
            .Where(c =>
                c.BillingPeriodId == period.Id &&
                c.ChargeType == FeeChargeType.Ordinary &&
                c.IsActive)
            .Select(c => c.UnitId)
            .ToListAsync(cancellationToken);
        var existingSet = existingUnitIds.ToHashSet();

        var dueDay = Math.Clamp(settings.DueDayOfMonth, 1, 28);
        var daysInMonth = DateTime.DaysInMonth(period.Year, period.Month);
        var dueDate = new DateOnly(period.Year, period.Month, Math.Min(dueDay, daysInMonth));

        var created = 0;
        var skipped = 0;
        var totalAmount = 0m;
        var newCharges = new List<FeeCharge>();

        foreach (var unit in units)
        {
            if (existingSet.Contains(unit.Id))
            {
                skipped++;
                continue;
            }

            CoefficientRules.EnsureValid(unit.Coefficient);
            var amount = Math.Round(
                settings.MonthlyBudgetAmount * (unit.Coefficient / CoefficientRules.Total),
                2,
                MidpointRounding.AwayFromZero);

            newCharges.Add(new FeeCharge
            {
                Id = Guid.NewGuid(),
                HorizontalPropertyId = dto.HorizontalPropertyId,
                BillingPeriodId = period.Id,
                UnitId = unit.Id,
                ChargeType = FeeChargeType.Ordinary,
                Description = $"Ordinary administration fee {period.Year:D4}-{period.Month:D2}",
                CoefficientSnapshot = unit.Coefficient,
                Amount = amount,
                AmountPaid = 0m,
                DueDate = dueDate,
                Status = FeeChargeStatus.Pending,
                IsActive = true,
            });
            created++;
            totalAmount += amount;
        }

        if (newCharges.Count > 0)
        {
            await _charges.AddRangeAsync(newCharges, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new GenerateChargesResultDto
        {
            CreatedCount = created,
            SkippedCount = skipped,
            TotalAmount = totalAmount,
        };
    }

    public async Task<GenerateChargesResultDto> GenerateInterestAsync(
        GenerateInterestChargesDto dto,
        CancellationToken cancellationToken = default)
    {
        var propertyExists = await _properties.Query().AnyAsync(p => p.Id == dto.HorizontalPropertyId, cancellationToken);
        if (!propertyExists)
            throw new InvalidOperationException("Horizontal property was not found.");

        var settings = await _db.FeeSettings
            .FirstOrDefaultAsync(s => s.HorizontalPropertyId == dto.HorizontalPropertyId, cancellationToken);
        if (settings is null || settings.LateInterestMonthlyPercent <= 0m)
            throw new InvalidOperationException("LateInterestMonthlyPercent must be greater than zero.");

        var period = await ResolveOpenPeriodAsync(dto.HorizontalPropertyId, dto.BillingPeriodId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var overdueOrdinary = await _charges.Query().AsNoTracking()
            .Where(c =>
                c.HorizontalPropertyId == dto.HorizontalPropertyId &&
                c.IsActive &&
                c.ChargeType == FeeChargeType.Ordinary &&
                (c.Status == FeeChargeStatus.Pending || c.Status == FeeChargeStatus.Partial) &&
                c.DueDate < today &&
                c.Amount > c.AmountPaid)
            .ToListAsync(cancellationToken);

        var balancesByUnit = overdueOrdinary
            .GroupBy(c => c.UnitId)
            .Select(g => new
            {
                UnitId = g.Key,
                Balance = g.Sum(c => c.Amount - c.AmountPaid),
                Coefficient = g.OrderByDescending(c => c.DueDate).First().CoefficientSnapshot,
            })
            .Where(x => x.Balance > 0m)
            .ToList();

        var existingInterestUnitIds = await _charges.Query().AsNoTracking()
            .Where(c =>
                c.BillingPeriodId == period.Id &&
                c.ChargeType == FeeChargeType.Interest &&
                c.IsActive)
            .Select(c => c.UnitId)
            .ToListAsync(cancellationToken);
        var existingSet = existingInterestUnitIds.ToHashSet();

        var created = 0;
        var skipped = 0;
        var totalAmount = 0m;
        var newCharges = new List<FeeCharge>();
        var dueDay = Math.Clamp(settings.DueDayOfMonth, 1, 28);
        var daysInMonth = DateTime.DaysInMonth(period.Year, period.Month);
        var dueDate = new DateOnly(period.Year, period.Month, Math.Min(dueDay, daysInMonth));

        foreach (var item in balancesByUnit)
        {
            if (existingSet.Contains(item.UnitId))
            {
                skipped++;
                continue;
            }

            var amount = Math.Round(
                item.Balance * settings.LateInterestMonthlyPercent / 100m,
                2,
                MidpointRounding.AwayFromZero);
            if (amount <= 0m)
            {
                skipped++;
                continue;
            }

            newCharges.Add(new FeeCharge
            {
                Id = Guid.NewGuid(),
                HorizontalPropertyId = dto.HorizontalPropertyId,
                BillingPeriodId = period.Id,
                UnitId = item.UnitId,
                ChargeType = FeeChargeType.Interest,
                Description = $"Late interest {period.Year:D4}-{period.Month:D2}",
                CoefficientSnapshot = item.Coefficient,
                Amount = amount,
                AmountPaid = 0m,
                DueDate = dueDate,
                Status = FeeChargeStatus.Pending,
                IsActive = true,
            });
            created++;
            totalAmount += amount;
        }

        if (newCharges.Count > 0)
        {
            await _charges.AddRangeAsync(newCharges, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new GenerateChargesResultDto
        {
            CreatedCount = created,
            SkippedCount = skipped,
            TotalAmount = totalAmount,
        };
    }

    public async Task<ArrearsSummaryDto> GetArrearsSummaryAsync(
        Guid horizontalPropertyId,
        CancellationToken cancellationToken = default)
    {
        var propertyExists = await _properties.Query().AnyAsync(p => p.Id == horizontalPropertyId, cancellationToken);
        if (!propertyExists)
            throw new InvalidOperationException("Horizontal property was not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var openCharges = await _charges.Query().AsNoTracking()
            .Where(c =>
                c.HorizontalPropertyId == horizontalPropertyId &&
                c.IsActive &&
                (c.Status == FeeChargeStatus.Pending || c.Status == FeeChargeStatus.Partial) &&
                c.Amount > c.AmountPaid)
            .Select(c => new { c.UnitId, c.Status, c.Amount, c.AmountPaid, c.DueDate })
            .ToListAsync(cancellationToken);

        var totalPending = openCharges
            .Where(c => c.Status == FeeChargeStatus.Pending)
            .Sum(c => c.Amount - c.AmountPaid);
        var totalPartial = openCharges
            .Where(c => c.Status == FeeChargeStatus.Partial)
            .Sum(c => c.Amount - c.AmountPaid);
        var totalOverdue = openCharges
            .Where(c => c.DueDate < today)
            .Sum(c => c.Amount - c.AmountPaid);
        var unitCountInArrears = openCharges
            .Where(c => c.DueDate < today)
            .Select(c => c.UnitId)
            .Distinct()
            .Count();

        return new ArrearsSummaryDto
        {
            TotalPending = totalPending,
            TotalPartial = totalPartial,
            TotalOverdue = totalOverdue,
            UnitCountInArrears = unitCountInArrears,
        };
    }

    private async Task<BillingPeriod> ResolveOpenPeriodAsync(
        Guid horizontalPropertyId,
        Guid? billingPeriodId,
        CancellationToken cancellationToken)
    {
        if (billingPeriodId.HasValue)
        {
            var specified = await _periods.Query()
                .FirstOrDefaultAsync(
                    p => p.Id == billingPeriodId.Value && p.HorizontalPropertyId == horizontalPropertyId,
                    cancellationToken)
                ?? throw new InvalidOperationException("Billing period was not found for the horizontal property.");

            if (specified.Status != BillingPeriodStatus.Open)
                throw new InvalidOperationException("Billing period must be Open to generate interest charges.");

            return specified;
        }

        var open = await _periods.Query()
            .Where(p => p.HorizontalPropertyId == horizontalPropertyId && p.Status == BillingPeriodStatus.Open)
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No open billing period was found for the horizontal property.");

        return open;
    }

    private async Task EnsurePropertyUnitPeriodAsync(
        Guid horizontalPropertyId,
        Guid unitId,
        Guid billingPeriodId,
        CancellationToken cancellationToken)
    {
        var propertyExists = await _properties.Query().AnyAsync(p => p.Id == horizontalPropertyId, cancellationToken);
        if (!propertyExists)
            throw new InvalidOperationException("Horizontal property was not found.");

        var unit = await _units.Query().AsNoTracking().FirstOrDefaultAsync(u => u.Id == unitId, cancellationToken)
            ?? throw new InvalidOperationException("Unit was not found.");
        if (unit.HorizontalPropertyId != horizontalPropertyId)
            throw new InvalidOperationException("Unit does not belong to the horizontal property.");

        var period = await _periods.Query().AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == billingPeriodId, cancellationToken)
            ?? throw new InvalidOperationException("Billing period was not found.");
        if (period.HorizontalPropertyId != horizontalPropertyId)
            throw new InvalidOperationException("Billing period does not belong to the horizontal property.");
        if (period.Status != BillingPeriodStatus.Open)
            throw new InvalidOperationException("Billing period must be Open.");
    }

    private static FeeChargeDto Map(FeeCharge entity) =>
        new()
        {
            Id = entity.Id,
            HorizontalPropertyId = entity.HorizontalPropertyId,
            BillingPeriodId = entity.BillingPeriodId,
            UnitId = entity.UnitId,
            ChargeType = entity.ChargeType,
            Description = entity.Description,
            CoefficientSnapshot = entity.CoefficientSnapshot,
            Amount = entity.Amount,
            AmountPaid = entity.AmountPaid,
            DueDate = entity.DueDate,
            Status = entity.Status,
            IsActive = entity.IsActive,
        };
}
