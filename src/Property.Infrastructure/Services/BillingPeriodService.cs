using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Billing.BillingPeriods;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Common;

namespace Property.Infrastructure.Services;

public sealed class BillingPeriodService : IBillingPeriodService
{
    private readonly IRepository<BillingPeriod> _periods;
    private readonly IRepository<HorizontalProperty> _properties;
    private readonly IUnitOfWork _unitOfWork;

    public BillingPeriodService(
        IRepository<BillingPeriod> periods,
        IRepository<HorizontalProperty> properties,
        IUnitOfWork unitOfWork)
    {
        _periods = periods;
        _properties = properties;
        _unitOfWork = unitOfWork;
    }

    public Task<PagedResult<BillingPeriodDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default) =>
        ListQueryHelpers.ToPagedResultAsync(
            _periods.Query().AsNoTracking(),
            query,
            q => PagedEntityQueries.ShapeBillingPeriods(q, query),
            entities => Task.FromResult<IReadOnlyList<BillingPeriodDto>>(entities.Select(Map).ToList()),
            cancellationToken);

    public async Task<BillingPeriodDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _periods.Query().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<BillingPeriodDto> CreateAsync(CreateBillingPeriodDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Month is < 1 or > 12)
            throw new InvalidOperationException("Month must be between 1 and 12.");

        var propertyExists = await _properties.Query().AnyAsync(p => p.Id == dto.HorizontalPropertyId, cancellationToken);
        if (!propertyExists)
            throw new InvalidOperationException("Horizontal property was not found.");

        var duplicate = await _periods.Query().AnyAsync(
            p => p.HorizontalPropertyId == dto.HorizontalPropertyId && p.Year == dto.Year && p.Month == dto.Month,
            cancellationToken);
        if (duplicate)
            throw new InvalidOperationException("A billing period already exists for that year and month.");

        var entity = new BillingPeriod
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = dto.HorizontalPropertyId,
            Year = dto.Year,
            Month = dto.Month,
            Status = BillingPeriodStatus.Open,
            Notes = dto.Notes?.Trim(),
        };
        await _periods.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<BillingPeriodDto?> UpdateAsync(
        Guid id,
        UpdateBillingPeriodDto dto,
        CancellationToken cancellationToken = default)
    {
        var entity = await _periods.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        entity.Notes = dto.Notes?.Trim();
        entity.Status = dto.Status;
        _periods.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> SoftCloseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _periods.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return false;

        if (entity.Status == BillingPeriodStatus.Closed)
            return false;

        entity.Status = BillingPeriodStatus.Closed;
        _periods.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static BillingPeriodDto Map(BillingPeriod entity) =>
        new()
        {
            Id = entity.Id,
            HorizontalPropertyId = entity.HorizontalPropertyId,
            Year = entity.Year,
            Month = entity.Month,
            PeriodKey = $"{entity.Year:D4}-{entity.Month:D2}",
            Status = entity.Status,
            Notes = entity.Notes,
            IsLiquidated = entity.IsLiquidated,
            LiquidatedAtUtc = entity.LiquidatedAtUtc,
        };
}
