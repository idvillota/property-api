using System.ComponentModel.DataAnnotations;

namespace Property.Application.Features.Organization.TenantUsers;

public sealed class InviteTenantUserDto
{
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Required when the email is not yet registered. Ignored when linking an existing global user to this tenant.
    /// </summary>
    [MinLength(8)]
    [MaxLength(200)]
    public string? Password { get; set; }

    [MaxLength(200)]
    public string? DisplayName { get; set; }

    /// <summary>Manager or Accountant (Administrator cannot be assigned via invite).</summary>
    [Required]
    [RegularExpression("^(Manager|Accountant)$", ErrorMessage = "Role must be Manager or Accountant.")]
    public string Role { get; set; } = string.Empty;
}

public sealed class InvitedTenantUserDto
{
    public Guid UserId { get; set; }
    public Guid TenantUserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}

/// <summary>Member of the current tenant (Equipo list).</summary>
public sealed class TenantUserListItemDto
{
    public Guid TenantUserId { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    public DateTime CreatedAtUtc { get; set; }
}
