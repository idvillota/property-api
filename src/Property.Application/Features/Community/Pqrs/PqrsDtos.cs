using System.ComponentModel.DataAnnotations;
using Property.Domain.Enums;

namespace Property.Application.Features.Community.Pqrs;

public sealed class PqrsCaseDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public Guid UnitId { get; set; }
    public string? UnitCode { get; set; }
    public Guid CreatedByPersonId { get; set; }
    public string? CreatedByPersonName { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public PqrsCategory Category { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PqrsStatus Status { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public IReadOnlyList<Guid> AttachmentFileAssetIds { get; set; } = Array.Empty<Guid>();
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public IReadOnlyList<PqrsCommentDto> Comments { get; set; } = Array.Empty<PqrsCommentDto>();
}

public sealed class PqrsCommentDto
{
    public Guid Id { get; set; }
    public Guid PqrsCaseId { get; set; }
    public Guid? AuthorUserId { get; set; }
    public Guid? AuthorPersonId { get; set; }
    public string AuthorDisplayName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public DateTime PostedAtUtc { get; set; }
}

public sealed class CreatePqrsCaseDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    [Required]
    public Guid UnitId { get; set; }

    [Required]
    public Guid CreatedByPersonId { get; set; }

    [Required]
    public PqrsCategory Category { get; set; }

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    public IReadOnlyList<Guid>? AttachmentFileAssetIds { get; set; }

    public DateTime? DueAtUtc { get; set; }

    public Guid? AssignedToUserId { get; set; }
}

public sealed class UpdatePqrsCaseStatusDto
{
    [Required]
    public PqrsStatus Status { get; set; }

    public DateTime? DueAtUtc { get; set; }

    [MaxLength(4000)]
    public string? Comment { get; set; }
}

public sealed class AddPqrsCommentDto
{
    [Required]
    [MaxLength(4000)]
    public string Body { get; set; } = string.Empty;

    public bool IsInternal { get; set; }
}

public sealed class CreatePortalPqrsDto
{
    [Required]
    public Guid UnitId { get; set; }

    [Required]
    public PqrsCategory Category { get; set; }

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    public IReadOnlyList<Guid>? AttachmentFileAssetIds { get; set; }
}
