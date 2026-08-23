using Property.Domain.Enums;

namespace Property.Application.Features.Portal;

public sealed class PortalUnitCardDto
{
    public Guid UnitId { get; set; }
    public string Code { get; set; } = string.Empty;
    public OccupancyRole OccupancyRole { get; set; }
    public bool IsPrimary { get; set; }
}

public sealed class PortalQuickLinkDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool Available { get; set; }
}

public sealed class PortalHomeDto
{
    public Guid PersonId { get; set; }
    public string PersonFullName { get; set; } = string.Empty;
    public string? PropertyName { get; set; }
    public IReadOnlyList<PortalUnitCardDto> Units { get; set; } = [];
    public IReadOnlyList<PortalQuickLinkDto> QuickLinks { get; set; } = [];
}

/// <summary>BIL-010 — portal estado de cuenta (scoped to linked units).</summary>
public sealed class PortalAccountDto
{
    public Guid PersonId { get; set; }
    public string PersonFullName { get; set; } = string.Empty;
    public decimal OpenBalance { get; set; }
    public decimal OverdueBalance { get; set; }
    public int OpenChargeCount { get; set; }
    public IReadOnlyList<PortalAccountUnitDto> Units { get; set; } = [];
    public IReadOnlyList<PortalAccountPaymentDto> RecentPayments { get; set; } = [];
}

public sealed class PortalAccountUnitDto
{
    public Guid UnitId { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal OpenBalance { get; set; }
    public decimal OverdueBalance { get; set; }
    public int OpenChargeCount { get; set; }
}

public sealed class PortalAccountPaymentDto
{
    public Guid PaymentId { get; set; }
    public Guid UnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public PaymentMethod Method { get; set; }
}

public sealed class PortalAccountUnitDetailDto
{
    public Guid UnitId { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal OpenBalance { get; set; }
    public decimal OverdueBalance { get; set; }
    public IReadOnlyList<PortalAccountChargeDto> OpenCharges { get; set; } = [];
    public IReadOnlyList<PortalAccountPaymentDto> RecentPayments { get; set; } = [];
}

public sealed class PortalAccountChargeDto
{
    public Guid FeeChargeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? PeriodKey { get; set; }
    public FeeChargeType ChargeType { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }
    public DateOnly DueDate { get; set; }
    public FeeChargeStatus Status { get; set; }
}
