using Property.Domain.Common;

namespace Property.Domain.Entities;

/// <summary>BIL-012 — log of dunning reminder attempts (outbox enqueue).</summary>
public class DunningSendLog : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public Guid UnitId { get; set; }
    public Guid DunningRuleId { get; set; }
    public Guid? PersonId { get; set; }

    public string ToEmail { get; set; } = string.Empty;
    public int DaysOverdueSnapshot { get; set; }
    public decimal BalanceSnapshot { get; set; }
    public DateTime SentAtUtc { get; set; }
    public Guid? OutboxMessageId { get; set; }

    public HorizontalProperty? HorizontalProperty { get; set; }
    public Unit? Unit { get; set; }
    public DunningRule? DunningRule { get; set; }
    public Person? Person { get; set; }
}
