using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Amenities.CommonAreas;
using Property.Domain.Entities;
using Property.Infrastructure.Common;

namespace Property.Infrastructure.Services;

public sealed class CommonAreaService : ICommonAreaService
{
    private readonly IRepository<CommonArea> _areas;
    private readonly IRepository<HorizontalProperty> _properties;
    private readonly IUnitOfWork _unitOfWork;

    public CommonAreaService(
        IRepository<CommonArea> areas,
        IRepository<HorizontalProperty> properties,
        IUnitOfWork unitOfWork)
    {
        _areas = areas;
        _properties = properties;
        _unitOfWork = unitOfWork;
    }

    public Task<PagedResult<CommonAreaDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default) =>
        ListQueryHelpers.ToPagedResultAsync(
            _areas.Query().AsNoTracking(),
            query,
            q => PagedEntityQueries.ShapeCommonAreas(q, query),
            entities => Task.FromResult<IReadOnlyList<CommonAreaDto>>(entities.Select(Map).ToList()),
            cancellationToken);

    public async Task<CommonAreaDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _areas.Query().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<CommonAreaDto> CreateAsync(CreateCommonAreaDto dto, CancellationToken cancellationToken = default)
    {
        await EnsureHorizontalPropertyAsync(dto.HorizontalPropertyId, cancellationToken);
        ValidateDurations(dto.MinDurationMinutes, dto.MaxDurationMinutes);

        var entity = new CommonArea
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = dto.HorizontalPropertyId,
            Name = dto.Name.Trim(),
            AreaType = dto.AreaType,
            Capacity = dto.Capacity,
            RequiresDeposit = dto.RequiresDeposit,
            DepositAmount = dto.DepositAmount,
            RulesNotes = dto.RulesNotes?.Trim(),
            MinDurationMinutes = dto.MinDurationMinutes,
            MaxDurationMinutes = dto.MaxDurationMinutes,
            IsActive = true,
        };
        await _areas.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<CommonAreaDto?> UpdateAsync(Guid id, UpdateCommonAreaDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _areas.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        await EnsureHorizontalPropertyAsync(dto.HorizontalPropertyId, cancellationToken);
        ValidateDurations(dto.MinDurationMinutes, dto.MaxDurationMinutes);

        entity.HorizontalPropertyId = dto.HorizontalPropertyId;
        entity.Name = dto.Name.Trim();
        entity.AreaType = dto.AreaType;
        entity.Capacity = dto.Capacity;
        entity.RequiresDeposit = dto.RequiresDeposit;
        entity.DepositAmount = dto.DepositAmount;
        entity.RulesNotes = dto.RulesNotes?.Trim();
        entity.MinDurationMinutes = dto.MinDurationMinutes;
        entity.MaxDurationMinutes = dto.MaxDurationMinutes;
        entity.IsActive = dto.IsActive;
        _areas.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _areas.GetByIdAsync(id, cancellationToken);
        if (entity is null || !entity.IsActive)
            return false;

        entity.IsActive = false;
        _areas.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ValidateDurations(int? minDurationMinutes, int? maxDurationMinutes)
    {
        if (minDurationMinutes.HasValue &&
            maxDurationMinutes.HasValue &&
            maxDurationMinutes.Value < minDurationMinutes.Value)
            throw new InvalidOperationException("MaxDurationMinutes must be greater than or equal to MinDurationMinutes.");
    }

    private async Task EnsureHorizontalPropertyAsync(Guid horizontalPropertyId, CancellationToken cancellationToken)
    {
        var exists = await _properties.Query().AnyAsync(p => p.Id == horizontalPropertyId, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("Horizontal property was not found.");
    }

    private static CommonAreaDto Map(CommonArea entity) =>
        new()
        {
            Id = entity.Id,
            HorizontalPropertyId = entity.HorizontalPropertyId,
            Name = entity.Name,
            AreaType = entity.AreaType,
            Capacity = entity.Capacity,
            RequiresDeposit = entity.RequiresDeposit,
            DepositAmount = entity.DepositAmount,
            RulesNotes = entity.RulesNotes,
            MinDurationMinutes = entity.MinDurationMinutes,
            MaxDurationMinutes = entity.MaxDurationMinutes,
            IsActive = entity.IsActive,
        };
}
