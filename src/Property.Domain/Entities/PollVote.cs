using Property.Domain.Common;

namespace Property.Domain.Entities;

public class PollVote : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid PollId { get; set; }
    public Guid PollOptionId { get; set; }
    public Guid? PersonId { get; set; }
    public Guid? UnitId { get; set; }
    public Poll? Poll { get; set; }
    public PollOption? PollOption { get; set; }
}
