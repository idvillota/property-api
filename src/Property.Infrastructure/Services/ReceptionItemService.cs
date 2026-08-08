using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Reception.ReceptionItems;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Common;

namespace Property.Infrastructure.Services;

public sealed class ReceptionItemService : IReceptionItemService
{
    private readonly IRepository<ReceptionItem> _items;
    private readonly IRepository<HorizontalProperty> _properties;
    private readonly IRepository<Unit> _units;
    private readonly IRepository<Person> _people;
    private readonly IRepository<Employee> _employees;
    private readonly IUnitOfWork _unitOfWork;

    public ReceptionItemService(
        IRepository<ReceptionItem> items,
        IRepository<HorizontalProperty> properties,
        IRepository<Unit> units,
        IRepository<Person> people,
        IRepository<Employee> employees,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _properties = properties;
        _units = units;
        _people = people;
        _employees = employees;
        _unitOfWork = unitOfWork;
    }

    public Task<PagedResult<ReceptionItemDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default) =>
        ListQueryHelpers.ToPagedResultAsync(
            _items.Query().AsNoTracking(),
            query,
            q => PagedEntityQueries.ShapeReceptionItems(q, query),
            entities => Task.FromResult<IReadOnlyList<ReceptionItemDto>>(entities.Select(Map).ToList()),
            cancellationToken);

    public async Task<ReceptionItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _items.Query().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<ReceptionItemDto> CreateAsync(CreateReceptionItemDto dto, CancellationToken cancellationToken = default)
    {
        await EnsureRelationsAsync(
            dto.HorizontalPropertyId,
            dto.UnitId,
            dto.PersonId,
            dto.ReceivedByEmployeeId,
            deliveredByEmployeeId: null,
            cancellationToken);

        var entity = new ReceptionItem
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = dto.HorizontalPropertyId,
            UnitId = dto.UnitId,
            PersonId = dto.PersonId,
            ItemType = dto.ItemType,
            Status = dto.Status,
            ReceivedAtUtc = dto.ReceivedAtUtc,
            ReceivedByEmployeeId = dto.ReceivedByEmployeeId,
            CarrierOrSender = dto.CarrierOrSender?.Trim(),
            TrackingOrReference = dto.TrackingOrReference?.Trim(),
            RecipientName = dto.RecipientName?.Trim(),
            Notes = dto.Notes?.Trim(),
            IsActive = true,
        };
        await _items.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<ReceptionItemDto?> UpdateAsync(
        Guid id,
        UpdateReceptionItemDto dto,
        CancellationToken cancellationToken = default)
    {
        var entity = await _items.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        await EnsureRelationsAsync(
            dto.HorizontalPropertyId,
            dto.UnitId,
            dto.PersonId,
            dto.ReceivedByEmployeeId,
            dto.DeliveredByEmployeeId,
            cancellationToken);

        entity.HorizontalPropertyId = dto.HorizontalPropertyId;
        entity.UnitId = dto.UnitId;
        entity.PersonId = dto.PersonId;
        entity.ItemType = dto.ItemType;
        entity.Status = dto.Status;
        entity.ReceivedAtUtc = dto.ReceivedAtUtc;
        entity.ReceivedByEmployeeId = dto.ReceivedByEmployeeId;
        entity.DeliveredAtUtc = dto.DeliveredAtUtc;
        entity.DeliveredByEmployeeId = dto.DeliveredByEmployeeId;
        entity.CarrierOrSender = dto.CarrierOrSender?.Trim();
        entity.TrackingOrReference = dto.TrackingOrReference?.Trim();
        entity.RecipientName = dto.RecipientName?.Trim();
        entity.Notes = dto.Notes?.Trim();
        entity.IsActive = dto.IsActive;
        _items.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<ReceptionItemDto?> DeliverAsync(
        Guid id,
        DeliverReceptionItemDto dto,
        CancellationToken cancellationToken = default)
    {
        var entity = await _items.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        if (entity.Status is ReceptionItemStatus.Delivered or ReceptionItemStatus.Returned or ReceptionItemStatus.Cancelled)
            throw new InvalidOperationException($"Cannot deliver a reception item with status {entity.Status}.");

        if (dto.DeliveredByEmployeeId.HasValue)
            await EnsureEmployeeAsync(dto.DeliveredByEmployeeId.Value, cancellationToken);

        entity.Status = ReceptionItemStatus.Delivered;
        entity.DeliveredAtUtc = DateTime.UtcNow;
        entity.DeliveredByEmployeeId = dto.DeliveredByEmployeeId;
        if (dto.Notes is not null)
            entity.Notes = dto.Notes.Trim();

        _items.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<ReceptionItemDto?> ReturnAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _items.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        if (entity.Status is ReceptionItemStatus.Returned or ReceptionItemStatus.Cancelled)
            throw new InvalidOperationException($"Cannot return a reception item with status {entity.Status}.");

        entity.Status = ReceptionItemStatus.Returned;
        _items.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _items.GetByIdAsync(id, cancellationToken);
        if (entity is null || !entity.IsActive)
            return false;

        entity.IsActive = false;
        _items.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ReceptionPendingCountDto> GetPendingCountAsync(
        Guid horizontalPropertyId,
        CancellationToken cancellationToken = default)
    {
        var propertyExists = await _properties.Query().AnyAsync(p => p.Id == horizontalPropertyId, cancellationToken);
        if (!propertyExists)
            throw new InvalidOperationException("Horizontal property was not found.");

        var count = await _items.Query().AsNoTracking().CountAsync(
            e => e.IsActive
                 && e.HorizontalPropertyId == horizontalPropertyId
                 && e.Status == ReceptionItemStatus.PendingPickup,
            cancellationToken);

        return new ReceptionPendingCountDto { PendingPickupCount = count };
    }

    private async Task EnsureRelationsAsync(
        Guid horizontalPropertyId,
        Guid? unitId,
        Guid? personId,
        Guid? receivedByEmployeeId,
        Guid? deliveredByEmployeeId,
        CancellationToken cancellationToken)
    {
        var propertyExists = await _properties.Query().AnyAsync(p => p.Id == horizontalPropertyId, cancellationToken);
        if (!propertyExists)
            throw new InvalidOperationException("Horizontal property was not found.");

        if (unitId.HasValue)
        {
            var unit = await _units.Query().AsNoTracking().FirstOrDefaultAsync(u => u.Id == unitId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Unit was not found.");

            if (unit.HorizontalPropertyId != horizontalPropertyId)
                throw new InvalidOperationException("Unit must belong to the same horizontal property.");
        }

        if (personId.HasValue)
        {
            var personExists = await _people.Query().AnyAsync(p => p.Id == personId.Value, cancellationToken);
            if (!personExists)
                throw new InvalidOperationException("Person was not found.");
        }

        if (receivedByEmployeeId.HasValue)
            await EnsureEmployeeAsync(receivedByEmployeeId.Value, cancellationToken);

        if (deliveredByEmployeeId.HasValue)
            await EnsureEmployeeAsync(deliveredByEmployeeId.Value, cancellationToken);
    }

    private async Task EnsureEmployeeAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var exists = await _employees.Query().AnyAsync(e => e.Id == employeeId, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("Employee was not found.");
    }

    private static ReceptionItemDto Map(ReceptionItem entity) =>
        new()
        {
            Id = entity.Id,
            HorizontalPropertyId = entity.HorizontalPropertyId,
            UnitId = entity.UnitId,
            PersonId = entity.PersonId,
            ItemType = entity.ItemType,
            Status = entity.Status,
            ReceivedAtUtc = entity.ReceivedAtUtc,
            ReceivedByEmployeeId = entity.ReceivedByEmployeeId,
            DeliveredAtUtc = entity.DeliveredAtUtc,
            DeliveredByEmployeeId = entity.DeliveredByEmployeeId,
            CarrierOrSender = entity.CarrierOrSender,
            TrackingOrReference = entity.TrackingOrReference,
            RecipientName = entity.RecipientName,
            Notes = entity.Notes,
            IsActive = entity.IsActive,
        };
}
