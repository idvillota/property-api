using Property.Domain.Enums;

namespace Property.Application.Features.Community;

public sealed class LibraryDocumentDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Guid FileAssetId { get; set; }
    public LibraryVisibility Visibility { get; set; }
}

public sealed class CreateLibraryDocumentDto
{
    public Guid HorizontalPropertyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Guid FileAssetId { get; set; }
    public LibraryVisibility Visibility { get; set; } = LibraryVisibility.Owners;
}

public sealed class MaintenancePlanDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Responsible { get; set; }
    public MaintenanceFrequency Frequency { get; set; }
    public IReadOnlyList<MaintenanceTaskDto> Tasks { get; set; } = [];
}

public sealed class MaintenanceTaskDto
{
    public Guid Id { get; set; }
    public Guid MaintenancePlanId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public DateOnly? CompletedAt { get; set; }
    public MaintenanceTaskStatus Status { get; set; }
    public bool IsOverdue { get; set; }
}

public sealed class CreateMaintenancePlanDto
{
    public Guid HorizontalPropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Responsible { get; set; }
    public MaintenanceFrequency Frequency { get; set; } = MaintenanceFrequency.Monthly;
    public string FirstTaskTitle { get; set; } = string.Empty;
    public DateOnly FirstDueDate { get; set; }
}

public sealed class InsurancePolicyDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public string InsurerName { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public Guid? FileAssetId { get; set; }
    public bool ExpiringSoon { get; set; }
}

public sealed class CreateInsurancePolicyDto
{
    public Guid HorizontalPropertyId { get; set; }
    public string InsurerName { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public Guid? FileAssetId { get; set; }
}

public sealed class PollDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PollStatus Status { get; set; }
    public PollVoteScope VoteScope { get; set; }
    public bool ShowPartialResults { get; set; }
    public IReadOnlyList<PollOptionDto> Options { get; set; } = [];
}

public sealed class PollOptionDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int VoteCount { get; set; }
}

public sealed class CreatePollDto
{
    public Guid HorizontalPropertyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PollVoteScope VoteScope { get; set; } = PollVoteScope.PerUnit;
    public bool ShowPartialResults { get; set; } = true;
    public IReadOnlyList<string> Options { get; set; } = [];
}

public sealed class CastPollVoteDto
{
    public Guid PollId { get; set; }
    public Guid PollOptionId { get; set; }
    public Guid? PersonId { get; set; }
    public Guid? UnitId { get; set; }
}
