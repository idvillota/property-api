using Property.Domain.Common;

namespace Property.Domain.Entities;

public class Budget : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public int Year { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public HorizontalProperty? HorizontalProperty { get; set; }
    public ICollection<BudgetLine> Lines { get; set; } = new List<BudgetLine>();
}
