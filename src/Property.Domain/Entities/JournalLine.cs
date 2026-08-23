using Property.Domain.Common;

namespace Property.Domain.Entities;

public class JournalLine : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid JournalEntryId { get; set; }
    public Guid LedgerAccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Memo { get; set; }
    public JournalEntry? JournalEntry { get; set; }
    public LedgerAccount? LedgerAccount { get; set; }
}
