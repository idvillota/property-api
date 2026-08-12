using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Amenities.Reservations;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Common;

namespace Property.Infrastructure.Services;

public sealed class ReservationService : IReservationService
{
    private static readonly ReservationStatus[] OverlapStatuses =
    [
        ReservationStatus.Pending,
        ReservationStatus.Confirmed,
    ];

    private readonly IRepository<Reservation> _reservations;
    private readonly IRepository<CommonArea> _areas;
    private readonly IRepository<Unit> _units;
    private readonly IRepository<Person> _people;
    private readonly IRepository<HorizontalProperty> _properties;
    private readonly IUnitOfWork _unitOfWork;

    public ReservationService(
        IRepository<Reservation> reservations,
        IRepository<CommonArea> areas,
        IRepository<Unit> units,
        IRepository<Person> people,
        IRepository<HorizontalProperty> properties,
        IUnitOfWork unitOfWork)
    {
        _reservations = reservations;
        _areas = areas;
        _units = units;
        _people = people;
        _properties = properties;
        _unitOfWork = unitOfWork;
    }

    public Task<PagedResult<ReservationDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default) =>
        ListQueryHelpers.ToPagedResultAsync(
            _reservations.Query().AsNoTracking(),
            query,
            q => PagedEntityQueries.ShapeReservations(q, query),
            entities => Task.FromResult<IReadOnlyList<ReservationDto>>(entities.Select(Map).ToList()),
            cancellationToken);

    public async Task<ReservationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _reservations.Query().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<ReservationDto> CreateAsync(CreateReservationDto dto, CancellationToken cancellationToken = default)
    {
        ValidateWindow(dto.StartsAtUtc, dto.EndsAtUtc);
        var area = await EnsureRelationsAsync(
            dto.HorizontalPropertyId,
            dto.CommonAreaId,
            dto.UnitId,
            dto.PersonId,
            cancellationToken);

        if (dto.AttendeesCount > area.Capacity)
            throw new InvalidOperationException("La cantidad de asistentes supera el aforo de la zona común.");

        if (OverlapStatuses.Contains(dto.Status))
            await EnsureNoOverlapAsync(dto.CommonAreaId, dto.StartsAtUtc, dto.EndsAtUtc, excludeId: null, cancellationToken);

        var entity = new Reservation
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = dto.HorizontalPropertyId,
            CommonAreaId = dto.CommonAreaId,
            UnitId = dto.UnitId,
            PersonId = dto.PersonId,
            StartsAtUtc = dto.StartsAtUtc,
            EndsAtUtc = dto.EndsAtUtc,
            Status = dto.Status,
            AttendeesCount = dto.AttendeesCount,
            Notes = dto.Notes?.Trim(),
            DepositPaid = dto.DepositPaid,
            IsActive = true,
        };
        await _reservations.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<ReservationDto?> UpdateAsync(Guid id, UpdateReservationDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _reservations.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        ValidateWindow(dto.StartsAtUtc, dto.EndsAtUtc);
        var area = await EnsureRelationsAsync(
            dto.HorizontalPropertyId,
            dto.CommonAreaId,
            dto.UnitId,
            dto.PersonId,
            cancellationToken);

        if (dto.AttendeesCount > area.Capacity)
            throw new InvalidOperationException("La cantidad de asistentes supera el aforo de la zona común.");

        if (OverlapStatuses.Contains(dto.Status) && dto.IsActive)
            await EnsureNoOverlapAsync(dto.CommonAreaId, dto.StartsAtUtc, dto.EndsAtUtc, excludeId: id, cancellationToken);

        entity.HorizontalPropertyId = dto.HorizontalPropertyId;
        entity.CommonAreaId = dto.CommonAreaId;
        entity.UnitId = dto.UnitId;
        entity.PersonId = dto.PersonId;
        entity.StartsAtUtc = dto.StartsAtUtc;
        entity.EndsAtUtc = dto.EndsAtUtc;
        entity.Status = dto.Status;
        entity.AttendeesCount = dto.AttendeesCount;
        entity.Notes = dto.Notes?.Trim();
        entity.DepositPaid = dto.DepositPaid;
        entity.IsActive = dto.IsActive;
        _reservations.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<ReservationDto?> ChangeStatusAsync(
        Guid id,
        ChangeReservationStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        var entity = await _reservations.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        EnsureAllowedTransition(entity.Status, dto.Status);

        if (OverlapStatuses.Contains(dto.Status) && entity.IsActive)
            await EnsureNoOverlapAsync(entity.CommonAreaId, entity.StartsAtUtc, entity.EndsAtUtc, excludeId: id, cancellationToken);

        entity.Status = dto.Status;
        _reservations.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _reservations.GetByIdAsync(id, cancellationToken);
        if (entity is null || !entity.IsActive)
            return false;

        entity.IsActive = false;
        _reservations.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ValidateWindow(DateTime startsAtUtc, DateTime endsAtUtc)
    {
        if (endsAtUtc <= startsAtUtc)
            throw new InvalidOperationException("La hora de fin debe ser posterior a la hora de inicio.");
    }

    private static void EnsureAllowedTransition(ReservationStatus current, ReservationStatus next)
    {
        if (current == next)
            return;

        var allowed = current switch
        {
            ReservationStatus.Pending => next is ReservationStatus.Confirmed or ReservationStatus.Cancelled,
            ReservationStatus.Confirmed => next is ReservationStatus.Completed
                or ReservationStatus.Cancelled
                or ReservationStatus.NoShow,
            _ => false,
        };

        if (!allowed)
            throw new InvalidOperationException("No es posible cambiar la reserva a ese estado.");
    }

    private async Task EnsureNoOverlapAsync(
        Guid commonAreaId,
        DateTime startsAtUtc,
        DateTime endsAtUtc,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var overlap = await _reservations.Query().AsNoTracking().AnyAsync(
            e => e.IsActive
                 && e.CommonAreaId == commonAreaId
                 && (e.Status == ReservationStatus.Pending || e.Status == ReservationStatus.Confirmed)
                 && (!excludeId.HasValue || e.Id != excludeId.Value)
                 && e.StartsAtUtc < endsAtUtc
                 && e.EndsAtUtc > startsAtUtc,
            cancellationToken);

        if (overlap)
            throw new InvalidOperationException(
                "Ya hay otra reserva en ese horario para esta zona. Por favor elige otra fecha u hora.");
    }

    private async Task<CommonArea> EnsureRelationsAsync(
        Guid horizontalPropertyId,
        Guid commonAreaId,
        Guid unitId,
        Guid? personId,
        CancellationToken cancellationToken)
    {
        var propertyExists = await _properties.Query().AnyAsync(p => p.Id == horizontalPropertyId, cancellationToken);
        if (!propertyExists)
            throw new InvalidOperationException("No encontramos la propiedad horizontal.");

        var area = await _areas.Query().AsNoTracking().FirstOrDefaultAsync(a => a.Id == commonAreaId, cancellationToken)
            ?? throw new InvalidOperationException("No encontramos la zona común seleccionada.");

        if (area.HorizontalPropertyId != horizontalPropertyId)
            throw new InvalidOperationException("La zona común no pertenece a esta propiedad.");

        var unit = await _units.Query().AsNoTracking().FirstOrDefaultAsync(u => u.Id == unitId, cancellationToken)
            ?? throw new InvalidOperationException("No encontramos la unidad seleccionada.");

        if (unit.HorizontalPropertyId != horizontalPropertyId)
            throw new InvalidOperationException("La unidad no pertenece a esta propiedad.");

        if (unit.UnitType is not (UnitType.Apartment or UnitType.House))
            throw new InvalidOperationException("Solo puedes reservar desde un apartamento o una casa.");

        if (personId.HasValue)
        {
            var personExists = await _people.Query().AnyAsync(p => p.Id == personId.Value, cancellationToken);
            if (!personExists)
                throw new InvalidOperationException("No encontramos la persona seleccionada.");
        }

        return area;
    }

    private static ReservationDto Map(Reservation entity) =>
        new()
        {
            Id = entity.Id,
            HorizontalPropertyId = entity.HorizontalPropertyId,
            CommonAreaId = entity.CommonAreaId,
            UnitId = entity.UnitId,
            PersonId = entity.PersonId,
            StartsAtUtc = entity.StartsAtUtc,
            EndsAtUtc = entity.EndsAtUtc,
            Status = entity.Status,
            AttendeesCount = entity.AttendeesCount,
            Notes = entity.Notes,
            DepositPaid = entity.DepositPaid,
            IsActive = entity.IsActive,
        };
}
