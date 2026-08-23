using Property.Domain.Common;

namespace Property.Domain.Entities;

public class BankStatement : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public string AccountLabel { get; set; } = string.Empty;
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public HorizontalProperty? HorizontalProperty { get; set; }
    public ICollection<BankStatementLine> Lines { get; set; } = new List<BankStatementLine>();
}
