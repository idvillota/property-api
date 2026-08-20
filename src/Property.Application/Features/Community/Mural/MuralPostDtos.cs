using System.ComponentModel.DataAnnotations;
using Property.Domain.Enums;

namespace Property.Application.Features.Community.Mural;

public sealed class MuralPostDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public MuralPostKind Kind { get; set; }
    public MuralPostCategory Category { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactNote { get; set; }
    public string? AreaLabel { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitCode { get; set; }
    public Guid? AuthorUserId { get; set; }
    public string AuthorDisplayName { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public bool IsHidden { get; set; }
    public DateTime PublishedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
}

public sealed class CreateMuralPostDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    [Required]
    public MuralPostKind Kind { get; set; } = MuralPostKind.Community;

    [Required]
    public MuralPostCategory Category { get; set; } = MuralPostCategory.Other;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string Body { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? ContactPhone { get; set; }

    [EmailAddress]
    [MaxLength(320)]
    public string? ContactEmail { get; set; }

    [MaxLength(200)]
    public string? ContactNote { get; set; }

    [MaxLength(120)]
    public string? AreaLabel { get; set; }

    public Guid? UnitId { get; set; }

    public bool IsPinned { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }
}

public sealed class UpdateMuralPostDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    [Required]
    public MuralPostKind Kind { get; set; }

    [Required]
    public MuralPostCategory Category { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string Body { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? ContactPhone { get; set; }

    [EmailAddress]
    [MaxLength(320)]
    public string? ContactEmail { get; set; }

    [MaxLength(200)]
    public string? ContactNote { get; set; }

    [MaxLength(120)]
    public string? AreaLabel { get; set; }

    public Guid? UnitId { get; set; }

    public bool IsPinned { get; set; }

    public bool IsHidden { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }

    public bool IsActive { get; set; } = true;
}
