using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class ApInvoice : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public Guid VendorId { get; set; }
    public Guid ExpenseAccountId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal WithholdingAmount { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public ApInvoiceStatus Status { get; set; } = ApInvoiceStatus.Open;
    public WithholdingKind? WithholdingKind { get; set; }
    public bool IsActive { get; set; } = true;
    public Vendor? Vendor { get; set; }
    public LedgerAccount? ExpenseAccount { get; set; }
    public HorizontalProperty? HorizontalProperty { get; set; }
}
