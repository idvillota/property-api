using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class JournalEntry : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public DateTime EntryDate { get; set; }
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public JournalSourceType SourceType { get; set; }
    public Guid? SourceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsPosted { get; set; } = true;
    public HorizontalProperty? HorizontalProperty { get; set; }
    public ICollection<JournalLine> Lines { get; set; } = new List<JournalLine>();
}
