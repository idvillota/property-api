using Property.Domain.Common;

namespace Property.Domain.Entities;

public class PollOption : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid PollId { get; set; }
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public Poll? Poll { get; set; }
}
