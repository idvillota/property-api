using Property.Domain.Common;

namespace Property.Domain.Entities;

public class PettyCashExpense : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid PettyCashFundId { get; set; }
    public Guid? ExpenseAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime SpentAt { get; set; }
    public Guid SupportFileAssetId { get; set; }
    public bool IsActive { get; set; } = true;
    public PettyCashFund? PettyCashFund { get; set; }
    public FileAsset? SupportFileAsset { get; set; }
    public LedgerAccount? ExpenseAccount { get; set; }
}
