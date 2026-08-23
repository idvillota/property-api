using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Parking;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services.Parking;

public sealed class VisitorParkingService : IVisitorParkingService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenant;

    public VisitorParkingService(ApplicationDbContext db, ICurrentTenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<VisitorParkingSettingsDto> UpsertSettingsAsync(
        UpsertVisitorParkingSettingsDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.TotalSpots < 0 || dto.HourlyRate < 0)
            throw new InvalidOperationException("Cupos y tarifa deben ser no negativos.");

        var settings = await _db.VisitorParkingSettings
            .FirstOrDefaultAsync(s => s.HorizontalPropertyId == dto.HorizontalPropertyId, cancellationToken);
        if (settings is null)
        {
            settings = new VisitorParkingSettings
            {
                Id = Guid.NewGuid(),
                TenantId = RequireTenant(),
                HorizontalPropertyId = dto.HorizontalPropertyId,
                CreatedAtUtc = DateTime.UtcNow,
            };
            await _db.VisitorParkingSettings.AddAsync(settings, cancellationToken);
        }

        settings.TotalSpots = dto.TotalSpots;
        settings.HourlyRate = Math.Round(dto.HourlyRate, 2, MidpointRounding.AwayFromZero);
        settings.DailyCap = dto.DailyCap.HasValue
            ? Math.Round(dto.DailyCap.Value, 2, MidpointRounding.AwayFromZero)
            : null;
        settings.IsActive = true;
        settings.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return await MapSettingsAsync(settings, cancellationToken);
    }

    public async Task<VisitorParkingSettingsDto?> GetSettingsAsync(
        Guid horizontalPropertyId, CancellationToken cancellationToken = default)
    {
        var settings = await _db.VisitorParkingSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.HorizontalPropertyId == horizontalPropertyId, cancellationToken);
        return settings is null ? null : await MapSettingsAsync(settings, cancellationToken);
    }

    public async Task<ParkingShiftDto> OpenShiftAsync(
        OpenParkingShiftDto dto, CancellationToken cancellationToken = default)
    {
        var open = await _db.ParkingShifts.AnyAsync(
            s => s.HorizontalPropertyId == dto.HorizontalPropertyId && s.Status == ParkingShiftStatus.Open,
            cancellationToken);
        if (open)
            throw new InvalidOperationException("Ya hay un turno abierto.");

        var shift = new ParkingShift
        {
            Id = Guid.NewGuid(),
            TenantId = RequireTenant(),
            HorizontalPropertyId = dto.HorizontalPropertyId,
            OpenedAtUtc = DateTime.UtcNow,
            Status = ParkingShiftStatus.Open,
            CollectedTotal = 0,
            CreatedAtUtc = DateTime.UtcNow,
        };
        await _db.ParkingShifts.AddAsync(shift, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return MapShift(shift);
    }

    public async Task<ParkingShiftDto> CloseShiftAsync(
        CloseParkingShiftDto dto, CancellationToken cancellationToken = default)
    {
        var shift = await _db.ParkingShifts.FirstOrDefaultAsync(s => s.Id == dto.ShiftId, cancellationToken)
            ?? throw new InvalidOperationException("Turno no encontrado.");
        if (shift.Status == ParkingShiftStatus.Closed)
            throw new InvalidOperationException("El turno ya está cerrado.");

        var openTickets = await _db.ParkingTickets.AnyAsync(
            t => t.ParkingShiftId == shift.Id && t.Status == ParkingTicketStatus.Open, cancellationToken);
        if (openTickets)
            throw new InvalidOperationException("Hay tickets abiertos; cierre egresos antes del arqueo.");

        shift.Status = ParkingShiftStatus.Closed;
        shift.ClosedAtUtc = DateTime.UtcNow;
        shift.DeclaredCash = Math.Round(dto.DeclaredCash, 2, MidpointRounding.AwayFromZero);
        shift.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return MapShift(shift);
    }

    public async Task<ParkingShiftDto?> GetOpenShiftAsync(
        Guid horizontalPropertyId, CancellationToken cancellationToken = default)
    {
        var shift = await _db.ParkingShifts.AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.HorizontalPropertyId == horizontalPropertyId && s.Status == ParkingShiftStatus.Open,
                cancellationToken);
        return shift is null ? null : MapShift(shift);
    }

    public async Task<ParkingTicketDto> IssueTicketAsync(
        IssueParkingTicketDto dto, CancellationToken cancellationToken = default)
    {
        var settings = await _db.VisitorParkingSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.HorizontalPropertyId == dto.HorizontalPropertyId && s.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Configure tarifas de parqueadero visitante.");

        var occupied = await _db.ParkingTickets.CountAsync(
            t => t.HorizontalPropertyId == dto.HorizontalPropertyId && t.Status == ParkingTicketStatus.Open,
            cancellationToken);
        if (occupied >= settings.TotalSpots)
            throw new InvalidOperationException("No hay cupos libres de parqueadero visitante.");

        var shift = await _db.ParkingShifts
            .FirstOrDefaultAsync(
                s => s.HorizontalPropertyId == dto.HorizontalPropertyId && s.Status == ParkingShiftStatus.Open,
                cancellationToken)
            ?? throw new InvalidOperationException("No hay turno de caja abierto.");

        var code = $"VP-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var ticket = new ParkingTicket
        {
            Id = Guid.NewGuid(),
            TenantId = RequireTenant(),
            HorizontalPropertyId = dto.HorizontalPropertyId,
            ParkingShiftId = shift.Id,
            Code = code,
            Plate = dto.Plate.Trim().ToUpperInvariant(),
            EnteredAtUtc = DateTime.UtcNow,
            Status = ParkingTicketStatus.Open,
            CreatedAtUtc = DateTime.UtcNow,
        };
        await _db.ParkingTickets.AddAsync(ticket, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return MapTicket(ticket);
    }

    public async Task<ParkingTicketDto> ExitTicketAsync(
        ExitParkingTicketDto dto, CancellationToken cancellationToken = default)
    {
        var ticket = await _db.ParkingTickets.FirstOrDefaultAsync(t => t.Id == dto.TicketId, cancellationToken)
            ?? throw new InvalidOperationException("Ticket no encontrado.");
        if (ticket.Status != ParkingTicketStatus.Open)
            throw new InvalidOperationException("El ticket ya está cerrado.");

        var shift = await _db.ParkingShifts.FirstOrDefaultAsync(s => s.Id == ticket.ParkingShiftId, cancellationToken)
            ?? throw new InvalidOperationException("Turno no encontrado.");
        if (shift.Status != ParkingShiftStatus.Open)
            throw new InvalidOperationException("El turno está cerrado e inmutable.");

        var settings = await _db.VisitorParkingSettings.AsNoTracking()
            .FirstAsync(s => s.HorizontalPropertyId == ticket.HorizontalPropertyId, cancellationToken);

        var exited = DateTime.UtcNow;
        var fee = CalculateFee(settings.HourlyRate, ticket.EnteredAtUtc, exited, settings.DailyCap);
        ticket.ExitedAtUtc = exited;
        ticket.FeeAmount = fee;
        ticket.Status = ParkingTicketStatus.Closed;
        ticket.UpdatedAtUtc = DateTime.UtcNow;
        shift.CollectedTotal = Math.Round(shift.CollectedTotal + fee, 2, MidpointRounding.AwayFromZero);
        shift.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return MapTicket(ticket);
    }

    public decimal CalculateFee(decimal hourlyRate, DateTime enteredAtUtc, DateTime exitedAtUtc, decimal? dailyCap)
    {
        var hours = Math.Max(1m, (decimal)Math.Ceiling((exitedAtUtc - enteredAtUtc).TotalHours));
        var fee = Math.Round(hours * hourlyRate, 2, MidpointRounding.AwayFromZero);
        if (dailyCap.HasValue)
            fee = Math.Min(fee, dailyCap.Value);
        return fee;
    }

    private async Task<VisitorParkingSettingsDto> MapSettingsAsync(
        VisitorParkingSettings s, CancellationToken cancellationToken)
    {
        var occupied = await _db.ParkingTickets.CountAsync(
            t => t.HorizontalPropertyId == s.HorizontalPropertyId && t.Status == ParkingTicketStatus.Open,
            cancellationToken);
        return new VisitorParkingSettingsDto
        {
            Id = s.Id,
            HorizontalPropertyId = s.HorizontalPropertyId,
            TotalSpots = s.TotalSpots,
            HourlyRate = s.HourlyRate,
            DailyCap = s.DailyCap,
            OccupiedSpots = occupied,
            IsActive = s.IsActive,
        };
    }

    private Guid RequireTenant() =>
        _tenant.TenantId is Guid id && id != Guid.Empty
            ? id
            : throw new InvalidOperationException("Tenant context is required.");

    private static ParkingShiftDto MapShift(ParkingShift s) => new()
    {
        Id = s.Id,
        HorizontalPropertyId = s.HorizontalPropertyId,
        Status = s.Status,
        OpenedAtUtc = s.OpenedAtUtc,
        ClosedAtUtc = s.ClosedAtUtc,
        CollectedTotal = s.CollectedTotal,
        DeclaredCash = s.DeclaredCash,
    };

    private static ParkingTicketDto MapTicket(ParkingTicket t) => new()
    {
        Id = t.Id,
        ParkingShiftId = t.ParkingShiftId,
        Code = t.Code,
        Plate = t.Plate,
        EnteredAtUtc = t.EnteredAtUtc,
        ExitedAtUtc = t.ExitedAtUtc,
        FeeAmount = t.FeeAmount,
        Status = t.Status,
    };
}
