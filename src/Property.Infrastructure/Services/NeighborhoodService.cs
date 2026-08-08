using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Structure.Neighborhoods;
using Property.Domain.Entities;
using Property.Infrastructure.Common;

namespace Property.Infrastructure.Services;

public sealed class NeighborhoodService : INeighborhoodService
{
    private readonly IRepository<Neighborhood> _neighborhoods;
    private readonly IUnitOfWork _unitOfWork;

    public NeighborhoodService(IRepository<Neighborhood> neighborhoods, IUnitOfWork unitOfWork)
    {
        _neighborhoods = neighborhoods;
        _unitOfWork = unitOfWork;
    }

    public Task<PagedResult<NeighborhoodDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default) =>
        ListQueryHelpers.ToPagedResultAsync(
            _neighborhoods.Query().AsNoTracking(),
            query,
            q => PagedEntityQueries.ShapeNeighborhoods(q, query),
            entities => Task.FromResult<IReadOnlyList<NeighborhoodDto>>(entities.Select(Map).ToList()),
            cancellationToken);

    public async Task<NeighborhoodDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _neighborhoods.Query().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<NeighborhoodDto> CreateAsync(CreateNeighborhoodDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new Neighborhood
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            City = dto.City?.Trim(),
            Country = string.IsNullOrWhiteSpace(dto.Country) ? "Colombia" : dto.Country.Trim(),
            IsActive = true,
        };
        await _neighborhoods.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<NeighborhoodDto?> UpdateAsync(Guid id, UpdateNeighborhoodDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _neighborhoods.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        entity.Name = dto.Name.Trim();
        entity.City = dto.City?.Trim();
        entity.Country = string.IsNullOrWhiteSpace(dto.Country) ? "Colombia" : dto.Country.Trim();
        entity.IsActive = dto.IsActive;
        _neighborhoods.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _neighborhoods.GetByIdAsync(id, cancellationToken);
        if (entity is null || !entity.IsActive)
            return false;

        entity.IsActive = false;
        _neighborhoods.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static NeighborhoodDto Map(Neighborhood entity) =>
        new()
        {
            Id = entity.Id,
            Name = entity.Name,
            City = entity.City,
            Country = entity.Country,
            IsActive = entity.IsActive,
        };
}
