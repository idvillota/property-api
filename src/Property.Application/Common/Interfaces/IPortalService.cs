using Property.Application.Features.Organization.UserPersonLinks;
using Property.Application.Features.Portal;

namespace Property.Application.Common.Interfaces;

public interface IPortalService
{
    Task<PortalMeDto> GetMeAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<PortalHomeDto> GetHomeAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>BIL-010 — account statement for linked units only.</summary>
    Task<PortalAccountDto> GetAccountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>BIL-010 — unit detail; throws UnauthorizedAccessException if out of scope.</summary>
    Task<PortalAccountUnitDetailDto> GetAccountUnitAsync(
        Guid userId,
        Guid unitId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns true when unitId is in the caller's linked active occupancies.</summary>
    Task<bool> IsUnitInScopeAsync(Guid userId, Guid unitId, CancellationToken cancellationToken = default);
}
