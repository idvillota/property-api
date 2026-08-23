using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class BankStatementLine : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid BankStatementId { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public BankLineMatchStatus MatchStatus { get; set; } = BankLineMatchStatus.Unmatched;
    public Guid? MatchedPaymentId { get; set; }
    public Guid? MatchedJournalEntryId { get; set; }
    public BankStatement? BankStatement { get; set; }
    public Payment? MatchedPayment { get; set; }
    public JournalEntry? MatchedJournalEntry { get; set; }
}
