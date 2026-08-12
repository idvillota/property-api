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
    private readonly IReceptionNotificationService _notifications;

    public ReceptionItemService(
        IRepository<ReceptionItem> items,
        IRepository<HorizontalProperty> properties,
        IRepository<Unit> units,
        IRepository<Person> people,
        IRepository<UnitOccupancy> occupancies,
        IRepository<Employee> employees,
        IUnitOfWork unitOfWork,
        IReceptionNotificationService notifications)
    {
        _items = items;
        _properties = properties;
        _units = units;
        _people = people;
        _occupancies = occupancies;
        _employees = employees;
        _unitOfWork = unitOfWork;
        _notifications = notifications;
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

        var habitant = await EnsureUnitHabitantAsync(
            dto.UnitId,
            dto.PersonId,
            dto.Direction == ReceptionDirection.Outbound
                ? "Quien deja el artículo debe ser un habitante activo de la unidad."
                : "El destinatario debe ser un habitante activo de la unidad.",
            cancellationToken);

        var (pickup1, pickup2) = NormalizeAuthorizedPickups(dto.Direction, dto.AuthorizedPickupName1, dto.AuthorizedPickupName2);

        if (dto.ReceivedByEmployeeId.HasValue)
            await EnsureEmployeeAsync(dto.ReceivedByEmployeeId.Value, cancellationToken);

        EnsureManualItemType(dto.ItemType);

        var entity = new ReceptionItem
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = dto.HorizontalPropertyId,
            UnitId = dto.UnitId,
            PersonId = dto.PersonId,
            Direction = dto.Direction,
            ItemType = dto.ItemType,
            Status = dto.Status,
            ReceivedAtUtc = DateTime.UtcNow,
            ReceivedByEmployeeId = dto.ReceivedByEmployeeId,
            CarrierOrSender = dto.Direction == ReceptionDirection.Inbound
                ? dto.CarrierOrSender?.Trim()
                : null,
            TrackingOrReference = dto.Direction == ReceptionDirection.Inbound
                ? dto.TrackingOrReference?.Trim()
                : null,
            RecipientName = PersonDisplayName(habitant),
            AuthorizedPickupName1 = pickup1,
            AuthorizedPickupName2 = pickup2,
            Notes = dto.Notes?.Trim(),
            IsActive = true,
        };
        await _items.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _notifications.NotifyItemStartedAsync(entity.Id, cancellationToken);
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

        var habitant = await EnsureUnitHabitantAsync(
            dto.UnitId,
            dto.PersonId,
            dto.Direction == ReceptionDirection.Outbound
                ? "Quien deja el artículo debe ser un habitante activo de la unidad."
                : "El destinatario debe ser un habitante activo de la unidad.",
            cancellationToken);

        var (pickup1, pickup2) = NormalizeAuthorizedPickups(dto.Direction, dto.AuthorizedPickupName1, dto.AuthorizedPickupName2);

        if (dto.ReceivedByEmployeeId.HasValue)
            await EnsureEmployeeAsync(dto.ReceivedByEmployeeId.Value, cancellationToken);

        if (dto.DeliveredByEmployeeId.HasValue)
            await EnsureEmployeeAsync(dto.DeliveredByEmployeeId.Value, cancellationToken);

        if (dto.Direction == ReceptionDirection.Inbound && dto.DeliveredToPersonId.HasValue)
            await EnsureUnitHabitantAsync(
                dto.UnitId,
                dto.DeliveredToPersonId.Value,
                "Quien recibe debe ser un habitante activo de la unidad.",
                cancellationToken);

        string? deliveredExternal = null;
        if (dto.Direction == ReceptionDirection.Outbound && !string.IsNullOrWhiteSpace(dto.DeliveredToExternalName))
        {
            deliveredExternal = MatchAuthorizedPickup(pickup1, pickup2, dto.DeliveredToExternalName)
                ?? throw new InvalidOperationException(
                    "Quien recibe debe ser uno de los receptores autorizados.");
        }

        if (dto.DeliveredAtUtc.HasValue && dto.DeliveredAtUtc != entity.DeliveredAtUtc)
            EnsureNotBeforeToday(dto.DeliveredAtUtc.Value, "La fecha de entrega");

        if (dto.DeliveredAtUtc.HasValue && dto.DeliveredAtUtc.Value < entity.ReceivedAtUtc)
            throw new InvalidOperationException("La fecha de entrega no puede ser anterior a la recepción.");

        if (IsUtilityBillType(entity.ItemType))
        {
            if (dto.ItemType != entity.ItemType)
                throw new InvalidOperationException(
                    "Las facturas públicas se gestionan con el registro masivo; no se puede cambiar el tipo.");
        }
        else
        {
            EnsureManualItemType(dto.ItemType);
        }

        entity.HorizontalPropertyId = dto.HorizontalPropertyId;
        entity.UnitId = dto.UnitId;
        entity.PersonId = dto.PersonId;
        entity.Direction = dto.Direction;
        entity.ItemType = dto.ItemType;
        entity.Status = dto.Status;
        entity.ReceivedByEmployeeId = dto.ReceivedByEmployeeId;
        entity.DeliveredAtUtc = dto.DeliveredAtUtc;
        entity.DeliveredByEmployeeId = dto.DeliveredByEmployeeId;
        entity.DeliveredToPersonId = dto.Direction == ReceptionDirection.Inbound ? dto.DeliveredToPersonId : null;
        entity.DeliveredToExternalName = dto.Direction == ReceptionDirection.Outbound ? deliveredExternal : null;
        entity.CarrierOrSender = dto.Direction == ReceptionDirection.Inbound
            ? dto.CarrierOrSender?.Trim()
            : null;
        entity.TrackingOrReference = dto.Direction == ReceptionDirection.Inbound
            ? dto.TrackingOrReference?.Trim()
            : null;
        entity.RecipientName = PersonDisplayName(habitant);
        entity.AuthorizedPickupName1 = pickup1;
        entity.AuthorizedPickupName2 = pickup2;
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

        if (dto.DeliveredByEmployeeId.HasValue)
            await EnsureEmployeeAsync(dto.DeliveredByEmployeeId.Value, cancellationToken);

        var deliveredAtUtc = DateTime.UtcNow;
        EnsureNotBeforeToday(deliveredAtUtc, "La fecha de entrega");
        if (deliveredAtUtc < entity.ReceivedAtUtc)
            throw new InvalidOperationException("La fecha de entrega no puede ser anterior a la recepción.");

        if (entity.Direction == ReceptionDirection.Inbound)
        {
            if (!dto.DeliveredToPersonId.HasValue || dto.DeliveredToPersonId.Value == Guid.Empty)
                throw new InvalidOperationException("Selecciona el habitante que recibe el ítem.");

            await EnsureUnitHabitantAsync(
                entity.UnitId.Value,
                dto.DeliveredToPersonId.Value,
                "Quien recibe debe ser un habitante activo de la unidad.",
                cancellationToken);

            entity.DeliveredToPersonId = dto.DeliveredToPersonId;
            entity.DeliveredToExternalName = null;
        }
        else
        {
            var matched = MatchAuthorizedPickup(
                entity.AuthorizedPickupName1,
                entity.AuthorizedPickupName2,
                dto.DeliveredToExternalName);
            if (matched is null)
                throw new InvalidOperationException(
                    "Selecciona uno de los receptores autorizados para la entrega.");

            entity.DeliveredToExternalName = matched;
            entity.DeliveredToPersonId = null;
        }

        entity.Status = ReceptionItemStatus.Delivered;
        entity.DeliveredAtUtc = deliveredAtUtc;
        entity.DeliveredByEmployeeId = dto.DeliveredByEmployeeId;
        if (dto.Notes is not null)
            entity.Notes = dto.Notes.Trim();

        _items.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _notifications.NotifyItemCompletedAsync(entity.Id, cancellationToken);
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

    public async Task<BulkUtilityBillsResultDto> CreateBulkUtilityBillsAsync(
        BulkUtilityBillsDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!IsUtilityBillType(dto.BillType))
            throw new InvalidOperationException(
                "El tipo debe ser factura de agua, energía, gas o administración.");

        await EnsurePropertyAsync(dto.HorizontalPropertyId, cancellationToken);

        var period = NormalizePeriod(dto.Period);
        var periodKey = BuildUtilityPeriodKey(dto.BillType, period);
        var billLabel = UtilityBillLabel(dto.BillType);
        var now = DateTime.UtcNow;

        var dwellings = await _units.Query().AsNoTracking()
            .Where(u =>
                u.IsActive &&
                u.HorizontalPropertyId == dto.HorizontalPropertyId &&
                (u.UnitType == UnitType.Apartment || u.UnitType == UnitType.House))
            .Select(u => new { u.Id })
            .ToListAsync(cancellationToken);

        var unitIds = dwellings.Select(d => d.Id).ToList();

        var existingUnitIds = await _items.Query().AsNoTracking()
            .Where(e =>
                e.IsActive &&
                e.HorizontalPropertyId == dto.HorizontalPropertyId &&
                e.Direction == ReceptionDirection.Inbound &&
                e.ItemType == dto.BillType &&
                e.TrackingOrReference == periodKey &&
                e.UnitId.HasValue &&
                unitIds.Contains(e.UnitId.Value))
            .Select(e => e.UnitId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        var existingSet = existingUnitIds.ToHashSet();

        var occupancies = await _occupancies.Query().AsNoTracking()
            .Where(o => o.IsActive && o.IsCommunicationResponsible && unitIds.Contains(o.UnitId))
            .Select(o => new { o.UnitId, o.PersonId, o.StartDate })
            .ToListAsync(cancellationToken);

        var habitantByUnit = occupancies
            .GroupBy(o => o.UnitId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(o => o.StartDate).Select(o => o.PersonId).First());

        var personIds = habitantByUnit.Values.Distinct().ToList();
        var people = personIds.Count == 0
            ? new Dictionary<Guid, Person>()
            : await _people.Query().AsNoTracking()
                .Where(p => personIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

        var created = 0;
        var skippedExists = 0;
        var skippedNoHabitant = 0;
        var createdIds = new List<Guid>();

        foreach (var dwelling in dwellings)
        {
            if (existingSet.Contains(dwelling.Id))
            {
                skippedExists++;
                continue;
            }

            if (!habitantByUnit.TryGetValue(dwelling.Id, out var personId) ||
                !people.TryGetValue(personId, out var person))
            {
                skippedNoHabitant++;
                continue;
            }

            var id = Guid.NewGuid();
            await _items.AddAsync(new ReceptionItem
            {
                Id = id,
                HorizontalPropertyId = dto.HorizontalPropertyId,
                UnitId = dwelling.Id,
                PersonId = personId,
                Direction = ReceptionDirection.Inbound,
                ItemType = dto.BillType,
                Status = ReceptionItemStatus.PendingPickup,
                ReceivedAtUtc = now,
                CarrierOrSender = billLabel,
                TrackingOrReference = periodKey,
                RecipientName = PersonDisplayName(person),
                Notes = $"Factura de {billLabel.ToLowerInvariant()} · período {period}",
                IsActive = true,
            }, cancellationToken);
            createdIds.Add(id);
            created++;
        }

        if (created > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var id in createdIds)
            await _notifications.NotifyItemStartedAsync(id, cancellationToken);

        return new BulkUtilityBillsResultDto
        {
            Period = period,
            BillType = dto.BillType,
            CreatedCount = created,
            SkippedAlreadyExistsCount = skippedExists,
            SkippedNoHabitantCount = skippedNoHabitant,
            DwellingUnitCount = dwellings.Count,
        };
    }

    private static bool IsUtilityBillType(ReceptionItemType type) =>
        type is ReceptionItemType.WaterBill
            or ReceptionItemType.EnergyBill
            or ReceptionItemType.GasBill
            or ReceptionItemType.AdministrationBill;

    private static void EnsureManualItemType(ReceptionItemType type)
    {
        if (IsUtilityBillType(type))
            throw new InvalidOperationException(
                "Las facturas públicas se registran con la operación masiva “Registrar facturas”, no ítem por ítem.");
    }

    private static string UtilityBillLabel(ReceptionItemType type) => type switch
    {
        ReceptionItemType.WaterBill => "Agua",
        ReceptionItemType.EnergyBill => "Energía",
        ReceptionItemType.GasBill => "Gas",
        ReceptionItemType.AdministrationBill => "Administración",
        _ => "Servicio",
    };

    private static string NormalizePeriod(string? period)
    {
        if (string.IsNullOrWhiteSpace(period))
            return $"{DateTime.UtcNow:yyyy-MM}";

        var trimmed = period.Trim();
        if (!System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d{4}-(0[1-9]|1[0-2])$"))
            throw new InvalidOperationException("El período debe tener el formato AAAA-MM.");

        return trimmed;
    }

    private static string BuildUtilityPeriodKey(ReceptionItemType billType, string period) =>
        $"FACTURA-{billType}-{period}";

    private async Task EnsurePropertyAsync(Guid horizontalPropertyId, CancellationToken cancellationToken)
    {
        var propertyExists = await _properties.Query().AnyAsync(p => p.Id == horizontalPropertyId, cancellationToken);
        if (!propertyExists)
            throw new InvalidOperationException("No encontramos la propiedad horizontal.");
    }

    private static void EnsureNotBeforeToday(DateTime utcMoment, string fieldLabel)
    {
        var utc = utcMoment.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(utcMoment, DateTimeKind.Utc)
            : utcMoment.ToUniversalTime();

        var minAllowed = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        if (DateOnly.FromDateTime(utc) < minAllowed)
            throw new InvalidOperationException($"{fieldLabel} no puede ser anterior a hoy.");
    }

    private static (string? Name1, string? Name2) NormalizeAuthorizedPickups(
        ReceptionDirection direction,
        string? name1,
        string? name2)
    {
        if (direction != ReceptionDirection.Outbound)
            return (null, null);

        var n1 = string.IsNullOrWhiteSpace(name1) ? null : name1.Trim();
        var n2 = string.IsNullOrWhiteSpace(name2) ? null : name2.Trim();

        if (n1 is null)
            throw new InvalidOperationException(
                "Indica al menos un receptor autorizado para el retiro.");

        if (n2 is not null && string.Equals(n1, n2, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Los dos receptores autorizados deben ser distintos.");

        return (n1, n2);
    }

    private static string? MatchAuthorizedPickup(string? name1, string? name2, string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return null;

        var value = candidate.Trim();
        if (name1 is not null && string.Equals(name1, value, StringComparison.OrdinalIgnoreCase))
            return name1;
        if (name2 is not null && string.Equals(name2, value, StringComparison.OrdinalIgnoreCase))
            return name2;
        return null;
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
        string notHabitantMessage,
        CancellationToken cancellationToken)
    {
        var person = await _people.Query().AsNoTracking().FirstOrDefaultAsync(p => p.Id == personId, cancellationToken)
            ?? throw new InvalidOperationException("No encontramos la persona seleccionada.");

        var isHabitant = await _occupancies.Query().AsNoTracking().AnyAsync(
            o => o.IsActive && o.UnitId == unitId && o.PersonId == personId,
            cancellationToken);

        if (!isHabitant)
            throw new InvalidOperationException(notHabitantMessage);

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
        IReadOnlyDictionary<Guid, string> people)
    {
        string? deliveredToDisplay = null;
        if (entity.Direction == ReceptionDirection.Outbound)
        {
            deliveredToDisplay = entity.DeliveredToExternalName;
        }
        else if (entity.DeliveredToPersonId is { } deliveredToId &&
                 people.TryGetValue(deliveredToId, out var deliveredName))
        {
            deliveredToDisplay = deliveredName;
        }

        return new ReceptionItemDto
        {
            Id = entity.Id,
            HorizontalPropertyId = entity.HorizontalPropertyId,
            UnitId = entity.UnitId,
            PersonId = entity.PersonId,
            Direction = entity.Direction,
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
            AuthorizedPickupName1 = entity.AuthorizedPickupName1,
            AuthorizedPickupName2 = entity.AuthorizedPickupName2,
            DeliveredToExternalName = entity.DeliveredToExternalName,
            Notes = entity.Notes,
            IsActive = entity.IsActive,
            UnitCode = entity.UnitId is { } unitId && units.TryGetValue(unitId, out var code) ? code : null,
            PersonName = entity.PersonId is { } personId && people.TryGetValue(personId, out var name)
                ? name
                : entity.RecipientName,
            DeliveredToPersonName = deliveredToDisplay,
        };
    }
}
