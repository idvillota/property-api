using Property.Application.Features.Organization.UserPersonLinks;

namespace Property.Application.Common.Interfaces;

public interface IUserPersonLinkService
{
    Task<UserPersonLinkDto?> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);

    Task<UserPersonLinkDto?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserPersonLinkDto> LinkAsync(Guid personId, LinkUserToPersonDto dto, CancellationToken cancellationToken = default);

    Task UnlinkAsync(Guid personId, CancellationToken cancellationToken = default);

    /// <summary>Active unit ids for the person linked to this user, or empty if no link.</summary>
    Task<(Guid? PersonId, IReadOnlyList<Guid> UnitIds)> GetPortalScopeAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PortalMeDto> GetPortalMeAsync(Guid userId, CancellationToken cancellationToken = default);
}
