using System.ComponentModel.DataAnnotations;
using Property.Domain.Enums;

namespace Property.Application.Features.Billing.BillingPeriods;

public sealed class BillingPeriodDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string PeriodKey { get; set; } = string.Empty;
    public BillingPeriodStatus Status { get; set; }
    public string? Notes { get; set; }
    public bool IsLiquidated { get; set; }
    public DateTime? LiquidatedAtUtc { get; set; }
}

public sealed class CreateBillingPeriodDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    [Range(2000, 2100)]
    public int Year { get; set; }

    [Range(1, 12)]
    public int Month { get; set; }

    [MaxLength(4000)]
    public string? Notes { get; set; }
}

public sealed class UpdateBillingPeriodDto
{
    [MaxLength(4000)]
    public string? Notes { get; set; }

    public BillingPeriodStatus Status { get; set; } = BillingPeriodStatus.Open;
}
