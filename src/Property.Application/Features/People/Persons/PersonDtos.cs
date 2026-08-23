using System.ComponentModel.DataAnnotations;
using Property.Domain.Enums;

namespace Property.Application.Features.People.Persons;

public sealed class PersonDto
{
    public Guid Id { get; set; }
    public DocumentType DocumentType { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool WhatsAppOptIn { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CreatePersonDto
{
    [Required]
    public DocumentType DocumentType { get; set; }

    [Required]
    [MaxLength(40)]
    public string DocumentNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(320)]
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(40)]
    public string? Phone { get; set; }

    public bool WhatsAppOptIn { get; set; }
}

public sealed class UpdatePersonDto
{
    [Required]
    public DocumentType DocumentType { get; set; }

    [Required]
    [MaxLength(40)]
    public string DocumentNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(320)]
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(40)]
    public string? Phone { get; set; }

    public bool WhatsAppOptIn { get; set; }

    public bool IsActive { get; set; } = true;
}
