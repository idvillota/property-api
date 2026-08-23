using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

/// <summary>PUC ledger account (ACC-001).</summary>
public class LedgerAccount : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public LedgerAccountType AccountType { get; set; }
    public string? ParentCode { get; set; }
    public bool IsPostable { get; set; } = true;
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
}
