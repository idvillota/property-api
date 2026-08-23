using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class ApPayment : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid ApInvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public PaymentMethod Method { get; set; }
    public string? Reference { get; set; }
    public bool IsActive { get; set; } = true;
    public ApInvoice? ApInvoice { get; set; }
}
