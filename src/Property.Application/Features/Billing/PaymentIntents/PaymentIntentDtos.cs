using System.ComponentModel.DataAnnotations;
using Property.Domain.Enums;

namespace Property.Application.Features.Billing.PaymentIntents;

public sealed class PaymentIntentAllocationDto
{
    public Guid FeeChargeId { get; set; }
    public decimal Amount { get; set; }
}

public sealed class PaymentIntentDto
{
    public Guid Id { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? ProviderRef { get; set; }
    public PaymentIntentStatus Status { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public Guid UnitId { get; set; }
    public Guid? PersonId { get; set; }
    public decimal Amount { get; set; }
    public Guid? PaymentId { get; set; }
    public string? FailureReason { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public IReadOnlyList<PaymentIntentAllocationDto> Allocations { get; set; } = [];
}

public sealed class CreatePaymentIntentAllocationDto
{
    [Required]
    public Guid FeeChargeId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
}

public sealed class CreatePaymentIntentDto
{
    [Required]
    [MaxLength(120)]
    public string IdempotencyKey { get; set; } = string.Empty;

    [Required]
    public Guid HorizontalPropertyId { get; set; }

    [Required]
    public Guid UnitId { get; set; }

    public Guid? PersonId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [MaxLength(120)]
    public string? ProviderRef { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreatePaymentIntentAllocationDto> Allocations { get; set; } = [];
}

public sealed class MarkPaymentIntentPaidDto
{
    [MaxLength(120)]
    public string? ProviderRef { get; set; }

    public DateTime? PaidAtUtc { get; set; }
}

public sealed class MarkPaymentIntentFailedDto
{
    [MaxLength(500)]
    public string? Reason { get; set; }
}
