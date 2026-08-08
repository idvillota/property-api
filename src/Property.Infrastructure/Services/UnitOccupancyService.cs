using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.People.UnitOccupancies;
using Property.Domain.Entities;
using Property.Infrastructure.Common;

namespace Property.Infrastructure.Services;

public sealed class UnitOccupancyService : IUnitOccupancyService
{
    private readonly IRepository<UnitOccupancy> _occupancies;
    private readonly IRepository<Unit> _units;
    private readonly IRepository<Person> _people;
    private readonly IUnitOfWork _unitOfWork;

    public UnitOccupancyService(
        IRepository<UnitOccupancy> occupancies,
        IRepository<Unit> units,
        IRepository<Person> people,
        IUnitOfWork unitOfWork)
    {
        _occupancies = occupancies;
        _units = units;
        _people = people;
        _unitOfWork = unitOfWork;
    }

    public Task<PagedResult<UnitOccupancyDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default) =>
        ListQueryHelpers.ToPagedResultAsync(
            _occupancies.Query().AsNoTracking(),
            query,
            q => PagedEntityQueries.ShapeUnitOccupancies(q, _units.Query().AsNoTracking(), query),
            entities => Task.FromResult<IReadOnlyList<UnitOccupancyDto>>(entities.Select(Map).ToList()),
            cancellationToken);

    public async Task<UnitOccupancyDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _occupancies.Query().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<UnitOccupancyDto> CreateAsync(CreateUnitOccupancyDto dto, CancellationToken cancellationToken = default)
    {
        ValidateDates(dto.StartDate, dto.EndDate);
        await EnsureUnitAndPersonAsync(dto.UnitId, dto.PersonId, cancellationToken);

        var entity = new UnitOccupancy
        {
            Id = Guid.NewGuid(),
            UnitId = dto.UnitId,
            PersonId = dto.PersonId,
            Role = dto.Role,
            IsPrimary = dto.IsPrimary,
            ReceivesBilling = dto.ReceivesBilling,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsActive = true,
        };
        await _occupancies.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<UnitOccupancyDto?> UpdateAsync(Guid id, UpdateUnitOccupancyDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _occupancies.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        ValidateDates(dto.StartDate, dto.EndDate);
        await EnsureUnitAndPersonAsync(dto.UnitId, dto.PersonId, cancellationToken);

        entity.UnitId = dto.UnitId;
        entity.PersonId = dto.PersonId;
        entity.Role = dto.Role;
        entity.IsPrimary = dto.IsPrimary;
        entity.ReceivesBilling = dto.ReceivesBilling;
        entity.StartDate = dto.StartDate;
        entity.EndDate = dto.EndDate;
        entity.IsActive = dto.IsActive;
        _occupancies.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _occupancies.GetByIdAsync(id, cancellationToken);
        if (entity is null || !entity.IsActive)
            return false;

        entity.IsActive = false;
        _occupancies.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ValidateDates(DateOnly startDate, DateOnly? endDate)
    {
        if (endDate.HasValue && endDate.Value < startDate)
            throw new InvalidOperationException("EndDate must be greater than or equal to StartDate.");
    }

    private async Task EnsureUnitAndPersonAsync(Guid unitId, Guid personId, CancellationToken cancellationToken)
    {
        var unit = await _units.Query().AsNoTracking().FirstOrDefaultAsync(u => u.Id == unitId, cancellationToken)
            ?? throw new InvalidOperationException("Unit was not found.");

        var person = await _people.Query().AsNoTracking().FirstOrDefaultAsync(p => p.Id == personId, cancellationToken)
            ?? throw new InvalidOperationException("Person was not found.");

        if (unit.TenantId != person.TenantId)
            throw new InvalidOperationException("Unit and Person must belong to the same tenant.");
    }

    private static UnitOccupancyDto Map(UnitOccupancy entity) =>
        new()
        {
            Id = entity.Id,
            UnitId = entity.UnitId,
            PersonId = entity.PersonId,
            Role = entity.Role,
            IsPrimary = entity.IsPrimary,
            ReceivesBilling = entity.ReceivesBilling,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            IsActive = entity.IsActive,
        };
}
