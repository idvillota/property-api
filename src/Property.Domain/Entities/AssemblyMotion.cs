using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class AssemblyMotion : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid AssemblyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AssemblyVoteMode VoteMode { get; set; } = AssemblyVoteMode.Coefficient;
    public AssemblyMotionStatus Status { get; set; } = AssemblyMotionStatus.Open;
    public Assembly? Assembly { get; set; }
    public ICollection<AssemblyVote> Votes { get; set; } = new List<AssemblyVote>();
}
