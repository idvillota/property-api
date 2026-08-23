namespace Property.Application.Features.Organization.UserPersonLinks;

public sealed class UserPersonLinkDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public Guid PersonId { get; set; }
    public string PersonFullName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public IReadOnlyList<Guid> UnitIds { get; set; } = Array.Empty<Guid>();
}

public sealed class LinkUserToPersonDto
{
    /// <summary>Existing user id, or omit and provide Email.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Lookup/create membership target by email when UserId is null.</summary>
    public string? Email { get; set; }
}

public sealed class PortalMeDto
{
    public Guid PersonId { get; set; }
    public string PersonFullName { get; set; } = string.Empty;
    public IReadOnlyList<Guid> UnitIds { get; set; } = Array.Empty<Guid>();
}
