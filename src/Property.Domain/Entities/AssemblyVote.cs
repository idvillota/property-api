using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class AssemblyVote : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid AssemblyMotionId { get; set; }
    public Guid UnitId { get; set; }
    public Guid? PersonId { get; set; }
    public AssemblyVoteChoice Choice { get; set; }
    public decimal Weight { get; set; } = 1m;
    public AssemblyMotion? AssemblyMotion { get; set; }
    public Unit? Unit { get; set; }
}
