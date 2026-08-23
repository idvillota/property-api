using Property.Domain.Common;

namespace Property.Domain.Entities;

public class BudgetLine : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid BudgetId { get; set; }
    public Guid LedgerAccountId { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
    public Budget? Budget { get; set; }
    public LedgerAccount? LedgerAccount { get; set; }
}
