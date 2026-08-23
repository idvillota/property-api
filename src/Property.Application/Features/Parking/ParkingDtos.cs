using Property.Domain.Enums;

namespace Property.Application.Features.Parking;

public sealed class VisitorParkingSettingsDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public int TotalSpots { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal? DailyCap { get; set; }
    public int OccupiedSpots { get; set; }
    public bool IsActive { get; set; }
}

public sealed class UpsertVisitorParkingSettingsDto
{
    public Guid HorizontalPropertyId { get; set; }
    public int TotalSpots { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal? DailyCap { get; set; }
}

public sealed class ParkingShiftDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public ParkingShiftStatus Status { get; set; }
    public DateTime OpenedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public decimal CollectedTotal { get; set; }
    public decimal? DeclaredCash { get; set; }
}

public sealed class ParkingTicketDto
{
    public Guid Id { get; set; }
    public Guid ParkingShiftId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Plate { get; set; } = string.Empty;
    public DateTime EnteredAtUtc { get; set; }
    public DateTime? ExitedAtUtc { get; set; }
    public decimal? FeeAmount { get; set; }
    public ParkingTicketStatus Status { get; set; }
}

public sealed class OpenParkingShiftDto
{
    public Guid HorizontalPropertyId { get; set; }
}

public sealed class CloseParkingShiftDto
{
    public Guid ShiftId { get; set; }
    public decimal DeclaredCash { get; set; }
}

public sealed class IssueParkingTicketDto
{
    public Guid HorizontalPropertyId { get; set; }
    public string Plate { get; set; } = string.Empty;
}

public sealed class ExitParkingTicketDto
{
    public Guid TicketId { get; set; }
}
