using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Structure.VehicleBrands;
using Property.Domain.Entities;
using Property.Infrastructure.Common;

namespace Property.Infrastructure.Services;

public sealed class VehicleBrandService : IVehicleBrandService
{
    private readonly IRepository<VehicleBrand> _brands;
    private readonly IUnitOfWork _unitOfWork;

    public VehicleBrandService(IRepository<VehicleBrand> brands, IUnitOfWork unitOfWork)
    {
        _brands = brands;
        _unitOfWork = unitOfWork;
    }

    public Task<PagedResult<VehicleBrandDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default) =>
        ListQueryHelpers.ToPagedResultAsync(
            _brands.Query().AsNoTracking(),
            query,
            q => PagedEntityQueries.ShapeVehicleBrands(q, query),
            entities => Task.FromResult<IReadOnlyList<VehicleBrandDto>>(entities.Select(Map).ToList()),
            cancellationToken);

    public async Task<VehicleBrandDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _brands.Query().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<VehicleBrandDto> CreateAsync(CreateVehicleBrandDto dto, CancellationToken cancellationToken = default)
    {
        var name = NormalizeName(dto.Name);
        await EnsureUniqueNameAsync(name, excludeId: null, cancellationToken);

        var entity = new VehicleBrand
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = true,
        };
        await _brands.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<VehicleBrandDto?> UpdateAsync(Guid id, UpdateVehicleBrandDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _brands.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        var name = NormalizeName(dto.Name);
        await EnsureUniqueNameAsync(name, excludeId: id, cancellationToken);

        entity.Name = name;
        entity.IsActive = dto.IsActive;
        _brands.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _brands.GetByIdAsync(id, cancellationToken);
        if (entity is null || !entity.IsActive)
            return false;

        entity.IsActive = false;
        _brands.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureUniqueNameAsync(string name, Guid? excludeId, CancellationToken cancellationToken)
    {
        var exists = await _brands.Query().AsNoTracking().AnyAsync(
            b => b.IsActive &&
                 b.Name == name &&
                 (!excludeId.HasValue || b.Id != excludeId.Value),
            cancellationToken);

        if (exists)
            throw new InvalidOperationException($"Ya existe una marca activa con el nombre {name}.");
    }

    private static string NormalizeName(string name) => name.Trim();

    private static VehicleBrandDto Map(VehicleBrand entity) =>
        new()
        {
            Id = entity.Id,
            Name = entity.Name,
            IsActive = entity.IsActive,
        };
}
