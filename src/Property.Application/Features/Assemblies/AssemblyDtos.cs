using Property.Domain.Enums;

namespace Property.Application.Features.Assemblies;

public sealed class AssemblyDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime ScheduledAtUtc { get; set; }
    public string? Location { get; set; }
    public AssemblyStatus Status { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public Guid? MinutesFileAssetId { get; set; }
    public decimal QuorumPercentRequired { get; set; }
    public IReadOnlyList<AgendaItemDto> AgendaItems { get; set; } = [];
}

public sealed class AgendaItemDto
{
    public Guid Id { get; set; }
    public int SortOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class CreateAssemblyDto
{
    public Guid HorizontalPropertyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime? ScheduledAtUtc { get; set; }
    public string? Location { get; set; }
    public decimal QuorumPercentRequired { get; set; } = 50m;
    public IReadOnlyList<CreateAgendaItemDto> AgendaItems { get; set; } = [];
}

public sealed class CreateAgendaItemDto
{
    public int SortOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class QuorumStatusDto
{
    public Guid AssemblyId { get; set; }
    public decimal TotalCoefficient { get; set; }
    public decimal PresentCoefficient { get; set; }
    public decimal ProxyCoefficient { get; set; }
    public decimal RepresentedCoefficient { get; set; }
    public decimal QuorumPercentRequired { get; set; }
    public decimal AchievedPercent { get; set; }
    public bool HasQuorum { get; set; }
}

public sealed class RegisterAttendanceDto
{
    public Guid AssemblyId { get; set; }
    public Guid UnitId { get; set; }
    public Guid? PersonId { get; set; }
}

public sealed class RegisterProxyDto
{
    public Guid AssemblyId { get; set; }
    public Guid GrantorUnitId { get; set; }
    public Guid ProxyPersonId { get; set; }
}

public sealed class CreateMotionDto
{
    public Guid AssemblyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AssemblyVoteMode VoteMode { get; set; } = AssemblyVoteMode.Coefficient;
}

public sealed class CastVoteDto
{
    public Guid MotionId { get; set; }
    public Guid UnitId { get; set; }
    public Guid? PersonId { get; set; }
    public AssemblyVoteChoice Choice { get; set; }
}

public sealed class MotionResultDto
{
    public Guid MotionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public AssemblyMotionStatus Status { get; set; }
    public AssemblyVoteMode VoteMode { get; set; }
    public decimal YesWeight { get; set; }
    public decimal NoWeight { get; set; }
    public decimal AbstainWeight { get; set; }
}

public sealed class AssemblyMotionDto
{
    public Guid Id { get; set; }
    public Guid AssemblyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AssemblyVoteMode VoteMode { get; set; }
    public AssemblyMotionStatus Status { get; set; }
}
