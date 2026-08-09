using Property.Domain.Enums;

namespace Property.Application.Features.People.Persons;

public sealed class PersonOverviewDto
{
    public Guid PersonId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string DocumentLabel { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public IReadOnlyList<PersonUnitLinkDto> Units { get; set; } = Array.Empty<PersonUnitLinkDto>();
    public PersonPaymentStatusDto PaymentStatus { get; set; } = new();
    public IReadOnlyList<PersonReservationItemDto> Reservations { get; set; } = Array.Empty<PersonReservationItemDto>();
}

public sealed class PersonUnitLinkDto
{
    public Guid UnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public UnitType UnitType { get; set; }
    public OccupancyRole Role { get; set; }
    public string? BuildingName { get; set; }
    public string? FloorName { get; set; }
    public bool IsPrimary { get; set; }
    public bool ReceivesBilling { get; set; }
    public decimal OpenBalance { get; set; }
    public decimal OverdueBalance { get; set; }
}

public sealed class PersonPaymentStatusDto
{
    public decimal OpenBalance { get; set; }
    public decimal OverdueBalance { get; set; }
    public decimal PaidLast90Days { get; set; }
    public int OpenChargeCount { get; set; }
    public IReadOnlyList<PersonPaymentItemDto> RecentPayments { get; set; } = Array.Empty<PersonPaymentItemDto>();
}

public sealed class PersonPaymentItemDto
{
    public Guid PaymentId { get; set; }
    public Guid UnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public PaymentMethod Method { get; set; }
}

public sealed class PersonReservationItemDto
{
    public Guid ReservationId { get; set; }
    public string CommonAreaName { get; set; } = string.Empty;
    public string UnitCode { get; set; } = string.Empty;
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public ReservationStatus Status { get; set; }
}
