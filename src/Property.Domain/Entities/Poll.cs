using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class Poll : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PollStatus Status { get; set; } = PollStatus.Draft;
    public PollVoteScope VoteScope { get; set; } = PollVoteScope.PerUnit;
    public bool ShowPartialResults { get; set; } = true;
    public HorizontalProperty? HorizontalProperty { get; set; }
    public ICollection<PollOption> Options { get; set; } = new List<PollOption>();
    public ICollection<PollVote> Votes { get; set; } = new List<PollVote>();
}
