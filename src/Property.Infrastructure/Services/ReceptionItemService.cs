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
    private readonly IRepository<UnitOccupancy> _occupancies;
    private readonly IRepository<Employee> _employees;
    private readonly IUnitOfWork _unitOfWork;

    public ReceptionItemService(
        IRepository<ReceptionItem> items,
        IRepository<HorizontalProperty> properties,
        IRepository<Unit> units,
        IRepository<Person> people,
        IRepository<UnitOccupancy> occupancies,
        IRepository<Employee> employees,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _properties = properties;
        _units = units;
        _people = people;
        _occupancies = occupancies;
        _employees = employees;
        _unitOfWork = unitOfWork;
    }

    public Task<PagedResult<ReceptionItemDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default) =>
        ListQueryHelpers.ToPagedResultAsync(
            _items.Query().AsNoTracking(),
            query,
            q => PagedEntityQueries.ShapeReceptionItems(q, query),
            entities => EnrichAsync(entities, cancellationToken),
            cancellationToken);

    public async Task<ReceptionItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _items.Query().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
            return null;

        var list = await EnrichAsync([entity], cancellationToken);
        return list[0];
    }

    public async Task<ReceptionItemDto> CreateAsync(CreateReceptionItemDto dto, CancellationToken cancellationToken = default)
    {
        await EnsurePropertyAsync(dto.HorizontalPropertyId, cancellationToken);
        await EnsureDwellingUnitAsync(dto.HorizontalPropertyId, dto.UnitId, cancellationToken);
        var recipient = await EnsureUnitHabitantAsync(dto.UnitId, dto.PersonId, cancellationToken);

        if (dto.ReceivedByEmployeeId.HasValue)
            await EnsureEmployeeAsync(dto.ReceivedByEmployeeId.Value, cancellationToken);

        EnsureNotBeforeToday(dto.ReceivedAtUtc, "La fecha de recepción");

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
            RecipientName = PersonDisplayName(recipient),
            Notes = dto.Notes?.Trim(),
            IsActive = true,
        };
        await _items.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var list = await EnrichAsync([entity], cancellationToken);
        return list[0];
    }

    public async Task<ReceptionItemDto?> UpdateAsync(
        Guid id,
        UpdateReceptionItemDto dto,
        CancellationToken cancellationToken = default)
    {
        var entity = await _items.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        await EnsurePropertyAsync(dto.HorizontalPropertyId, cancellationToken);
        await EnsureDwellingUnitAsync(dto.HorizontalPropertyId, dto.UnitId, cancellationToken);
        var recipient = await EnsureUnitHabitantAsync(dto.UnitId, dto.PersonId, cancellationToken);

        if (dto.ReceivedByEmployeeId.HasValue)
            await EnsureEmployeeAsync(dto.ReceivedByEmployeeId.Value, cancellationToken);

        if (dto.DeliveredByEmployeeId.HasValue)
            await EnsureEmployeeAsync(dto.DeliveredByEmployeeId.Value, cancellationToken);

        if (dto.DeliveredToPersonId.HasValue)
            await EnsureUnitHabitantAsync(dto.UnitId, dto.DeliveredToPersonId.Value, cancellationToken);

        if (dto.ReceivedAtUtc != entity.ReceivedAtUtc)
            EnsureNotBeforeToday(dto.ReceivedAtUtc, "La fecha de recepción");

        if (dto.DeliveredAtUtc.HasValue && dto.DeliveredAtUtc != entity.DeliveredAtUtc)
            EnsureNotBeforeToday(dto.DeliveredAtUtc.Value, "La fecha de entrega");

        if (dto.DeliveredAtUtc.HasValue && dto.DeliveredAtUtc.Value < dto.ReceivedAtUtc)
            throw new InvalidOperationException("La fecha de entrega no puede ser anterior a la recepción.");

        entity.HorizontalPropertyId = dto.HorizontalPropertyId;
        entity.UnitId = dto.UnitId;
        entity.PersonId = dto.PersonId;
        entity.ItemType = dto.ItemType;
        entity.Status = dto.Status;
        entity.ReceivedAtUtc = dto.ReceivedAtUtc;
        entity.ReceivedByEmployeeId = dto.ReceivedByEmployeeId;
        entity.DeliveredAtUtc = dto.DeliveredAtUtc;
        entity.DeliveredByEmployeeId = dto.DeliveredByEmployeeId;
        entity.DeliveredToPersonId = dto.DeliveredToPersonId;
        entity.CarrierOrSender = dto.CarrierOrSender?.Trim();
        entity.TrackingOrReference = dto.TrackingOrReference?.Trim();
        entity.RecipientName = PersonDisplayName(recipient);
        entity.Notes = dto.Notes?.Trim();
        entity.IsActive = dto.IsActive;
        _items.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var list = await EnrichAsync([entity], cancellationToken);
        return list[0];
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
            throw new InvalidOperationException("Este ítem ya no se puede marcar como entregado.");

        if (!entity.UnitId.HasValue)
            throw new InvalidOperationException("Asocia una unidad al ítem antes de entregarlo.");

        await EnsureUnitHabitantAsync(entity.UnitId.Value, dto.DeliveredToPersonId, cancellationToken);

        if (dto.DeliveredByEmployeeId.HasValue)
            await EnsureEmployeeAsync(dto.DeliveredByEmployeeId.Value, cancellationToken);

        var deliveredAtUtc = DateTime.UtcNow;
        EnsureNotBeforeToday(deliveredAtUtc, "La fecha de entrega");
        if (deliveredAtUtc < entity.ReceivedAtUtc)
            throw new InvalidOperationException("La fecha de entrega no puede ser anterior a la recepción.");

        entity.Status = ReceptionItemStatus.Delivered;
        entity.DeliveredAtUtc = deliveredAtUtc;
        entity.DeliveredToPersonId = dto.DeliveredToPersonId;
        entity.DeliveredByEmployeeId = dto.DeliveredByEmployeeId;
        if (dto.Notes is not null)
            entity.Notes = dto.Notes.Trim();

        _items.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var list = await EnrichAsync([entity], cancellationToken);
        return list[0];
    }

    public async Task<ReceptionItemDto?> ReturnAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _items.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        if (entity.Status is ReceptionItemStatus.Returned or ReceptionItemStatus.Cancelled)
            throw new InvalidOperationException("Este ítem ya no se puede marcar como devuelto.");

        entity.Status = ReceptionItemStatus.Returned;
        _items.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var list = await EnrichAsync([entity], cancellationToken);
        return list[0];
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
        await EnsurePropertyAsync(horizontalPropertyId, cancellationToken);

        var count = await _items.Query().AsNoTracking().CountAsync(
            e => e.IsActive
                 && e.HorizontalPropertyId == horizontalPropertyId
                 && e.Status == ReceptionItemStatus.PendingPickup,
            cancellationToken);

        return new ReceptionPendingCountDto { PendingPickupCount = count };
    }

    private async Task EnsurePropertyAsync(Guid horizontalPropertyId, CancellationToken cancellationToken)
    {
        var propertyExists = await _properties.Query().AnyAsync(p => p.Id == horizontalPropertyId, cancellationToken);
        if (!propertyExists)
            throw new InvalidOperationException("No encontramos la propiedad horizontal.");
    }

    /// <summary>
    /// Rejects moments clearly before "today", with 1 UTC-day slack for local timezones west of UTC.
    /// </summary>
    private static void EnsureNotBeforeToday(DateTime utcMoment, string fieldLabel)
    {
        var utc = utcMoment.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(utcMoment, DateTimeKind.Utc)
            : utcMoment.ToUniversalTime();

        var minAllowed = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        if (DateOnly.FromDateTime(utc) < minAllowed)
            throw new InvalidOperationException($"{fieldLabel} no puede ser anterior a hoy.");
    }

    private async Task EnsureDwellingUnitAsync(
        Guid horizontalPropertyId,
        Guid unitId,
        CancellationToken cancellationToken)
    {
        var unit = await _units.Query().AsNoTracking().FirstOrDefaultAsync(u => u.Id == unitId, cancellationToken)
            ?? throw new InvalidOperationException("No encontramos la unidad seleccionada.");

        if (unit.HorizontalPropertyId != horizontalPropertyId)
            throw new InvalidOperationException("La unidad no pertenece a esta propiedad.");

        if (unit.UnitType is not (UnitType.Apartment or UnitType.House))
            throw new InvalidOperationException("Solo puedes registrar portería para apartamentos o casas.");
    }

    private async Task<Person> EnsureUnitHabitantAsync(
        Guid unitId,
        Guid personId,
        CancellationToken cancellationToken)
    {
        var person = await _people.Query().AsNoTracking().FirstOrDefaultAsync(p => p.Id == personId, cancellationToken)
            ?? throw new InvalidOperationException("No encontramos la persona seleccionada.");

        var isHabitant = await _occupancies.Query().AsNoTracking().AnyAsync(
            o => o.IsActive && o.UnitId == unitId && o.PersonId == personId,
            cancellationToken);

        if (!isHabitant)
            throw new InvalidOperationException(
                "La persona debe ser un habitante activo de la unidad.");

        return person;
    }

    private async Task EnsureEmployeeAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var exists = await _employees.Query().AnyAsync(e => e.Id == employeeId, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("No encontramos el empleado seleccionado.");
    }

    private async Task<IReadOnlyList<ReceptionItemDto>> EnrichAsync(
        IEnumerable<ReceptionItem> entities,
        CancellationToken cancellationToken)
    {
        var list = entities as IReadOnlyList<ReceptionItem> ?? entities.ToList();
        if (list.Count == 0)
            return [];

        var unitIds = list.Where(e => e.UnitId.HasValue).Select(e => e.UnitId!.Value).Distinct().ToList();
        var personIds = list
            .SelectMany(e => new Guid?[] { e.PersonId, e.DeliveredToPersonId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var units = unitIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _units.Query().AsNoTracking()
                .Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Code, cancellationToken);

        var people = personIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _people.Query().AsNoTracking()
                .Where(p => personIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, PersonDisplayName, cancellationToken);

        return list.Select(e => Map(e, units, people)).ToList();
    }

    private static string PersonDisplayName(Person person) =>
        $"{person.FirstName} {person.LastName}".Trim();

    private static ReceptionItemDto Map(
        ReceptionItem entity,
        IReadOnlyDictionary<Guid, string> units,
        IReadOnlyDictionary<Guid, string> people) =>
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
            DeliveredToPersonId = entity.DeliveredToPersonId,
            CarrierOrSender = entity.CarrierOrSender,
            TrackingOrReference = entity.TrackingOrReference,
            RecipientName = entity.RecipientName,
            Notes = entity.Notes,
            IsActive = entity.IsActive,
            UnitCode = entity.UnitId is { } unitId && units.TryGetValue(unitId, out var code) ? code : null,
            PersonName = entity.PersonId is { } personId && people.TryGetValue(personId, out var name)
                ? name
                : entity.RecipientName,
            DeliveredToPersonName = entity.DeliveredToPersonId is { } deliveredToId
                && people.TryGetValue(deliveredToId, out var deliveredName)
                ? deliveredName
                : null,
        };
}
