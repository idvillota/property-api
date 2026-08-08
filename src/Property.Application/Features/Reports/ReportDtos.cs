using Property.Domain.Enums;

namespace Property.Application.Features.Reports;

public sealed class ReportOverviewDto
{
    public int UnitCount { get; set; }
    public int BillableUnitCount { get; set; }
    public int ActiveOccupancyCount { get; set; }
    public decimal TotalCoefficient { get; set; }
    public decimal RemainingCoefficientToHundred { get; set; }
    public decimal OpenChargesAmount { get; set; }
    public decimal OverdueChargesAmount { get; set; }
    public decimal CollectedAmountThisMonth { get; set; }
    public int PendingReceptionCount { get; set; }
    public int UpcomingReservationsCount { get; set; }
}

public sealed class CarteraTotalsDto
{
    public int UnitCount { get; set; }
    public decimal TotalBalance { get; set; }
    public decimal TotalOverdue { get; set; }
}

public sealed class CarteraItemDto
{
    public Guid UnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public UnitType UnitType { get; set; }
    public decimal Balance { get; set; }
    public decimal OverdueBalance { get; set; }
    public DateOnly? OldestDueDate { get; set; }
    public int ChargeCount { get; set; }
}

public sealed class CarteraReportDto
{
    public DateOnly AsOfDate { get; set; }
    public CarteraTotalsDto Totals { get; set; } = new();
    public IReadOnlyList<CarteraItemDto> Items { get; set; } = [];
}

public sealed class CollectionsTotalsDto
{
    public int PaymentCount { get; set; }
    public decimal TotalAmount { get; set; }
}

public sealed class CollectionsByMethodDto
{
    public PaymentMethod Method { get; set; }
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

public sealed class CollectionsByDayDto
{
    public DateOnly Date { get; set; }
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

public sealed class CollectionsItemDto
{
    public Guid PaymentId { get; set; }
    public Guid UnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public PaymentMethod Method { get; set; }
    public string? Reference { get; set; }
}

public sealed class CollectionsReportDto
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public CollectionsTotalsDto Totals { get; set; } = new();
    public IReadOnlyList<CollectionsByMethodDto> ByMethod { get; set; } = [];
    public IReadOnlyList<CollectionsByDayDto> ByDay { get; set; } = [];
    public IReadOnlyList<CollectionsItemDto> Items { get; set; } = [];
}

public sealed class OccupancyTotalsDto
{
    public int UnitCount { get; set; }
    public int UnitsWithOwner { get; set; }
    public int UnitsWithResident { get; set; }
    public int UnitsVacant { get; set; }
}

public sealed class OccupancyItemDto
{
    public Guid UnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public UnitType UnitType { get; set; }
    public bool IsBillable { get; set; }
    public string? OwnerName { get; set; }
    public string? ResidentName { get; set; }
    public int OccupancyCount { get; set; }
    public bool HasBillingContact { get; set; }
}

public sealed class OccupancyReportDto
{
    public OccupancyTotalsDto Totals { get; set; } = new();
    public IReadOnlyList<OccupancyItemDto> Items { get; set; } = [];
}

public sealed class CoefficientsTotalsDto
{
    public int UnitCount { get; set; }
    public int BillableUnitCount { get; set; }
    public decimal TotalCoefficient { get; set; }
    public decimal RemainingToHundred { get; set; }
}

public sealed class CoefficientItemDto
{
    public Guid UnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public UnitType UnitType { get; set; }
    public decimal Coefficient { get; set; }
    public decimal PrivateBuiltAreaM2 { get; set; }
    public bool IsBillable { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CoefficientsReportDto
{
    public CoefficientsTotalsDto Totals { get; set; } = new();
    public IReadOnlyList<CoefficientItemDto> Items { get; set; } = [];
}

public sealed class ReservationUsageTotalsDto
{
    public int ReservationCount { get; set; }
    public int ConfirmedCount { get; set; }
    public int CancelledCount { get; set; }
    public int CompletedCount { get; set; }
}

public sealed class ReservationUsageByAreaDto
{
    public Guid CommonAreaId { get; set; }
    public string CommonAreaName { get; set; } = string.Empty;
    public int Count { get; set; }
    public int ConfirmedCount { get; set; }
}

public sealed class ReservationUsageItemDto
{
    public Guid ReservationId { get; set; }
    public string CommonAreaName { get; set; } = string.Empty;
    public string UnitCode { get; set; } = string.Empty;
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public ReservationStatus Status { get; set; }
    public int AttendeesCount { get; set; }
}

public sealed class ReservationUsageReportDto
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public ReservationUsageTotalsDto Totals { get; set; } = new();
    public IReadOnlyList<ReservationUsageByAreaDto> ByArea { get; set; } = [];
    public IReadOnlyList<ReservationUsageItemDto> Items { get; set; } = [];
}

public sealed class ReceptionReportTotalsDto
{
    public int Total { get; set; }
    public int PendingPickup { get; set; }
    public int Delivered { get; set; }
    public int Returned { get; set; }
}

public sealed class ReceptionReportByTypeDto
{
    public ReceptionItemType ItemType { get; set; }
    public int Count { get; set; }
}

public sealed class ReceptionReportItemDto
{
    public Guid Id { get; set; }
    public ReceptionItemType ItemType { get; set; }
    public string? UnitCode { get; set; }
    public string? RecipientName { get; set; }
    public ReceptionItemStatus Status { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
}

public sealed class ReceptionReportDto
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public ReceptionReportTotalsDto Totals { get; set; } = new();
    public IReadOnlyList<ReceptionReportByTypeDto> ByType { get; set; } = [];
    public IReadOnlyList<ReceptionReportItemDto> Items { get; set; } = [];
}
