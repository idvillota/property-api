using Property.Application.Features.Community;
using Property.Domain.Enums;

namespace Property.Application.Common.Interfaces;

public interface ILibraryService
{
    Task<LibraryDocumentDto> CreateAsync(CreateLibraryDocumentDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LibraryDocumentDto>> ListAdminAsync(Guid horizontalPropertyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LibraryDocumentDto>> ListPortalAsync(Guid horizontalPropertyId, bool isOwner, CancellationToken cancellationToken = default);
}
