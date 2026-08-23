using Property.Application.Common.Models;
using Property.Application.Features.Community.Mural;

namespace Property.Application.Common.Interfaces;

public interface IMuralPostService
{
    Task<PagedResult<MuralPostDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default);

    /// <summary>Portal feed: active, not hidden, not expired; filtered by reader audience when scope provided.</summary>
    Task<IReadOnlyList<MuralPostDto>> ListPublicAsync(
        Guid? horizontalPropertyId = null,
        MuralReaderScope? readerScope = null,
        CancellationToken cancellationToken = default);

    Task<MuralPostDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MuralPostDto> CreateAsync(CreateMuralPostDto dto, CancellationToken cancellationToken = default);
    Task<MuralPostDto?> UpdateAsync(Guid id, UpdateMuralPostDto dto, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<MuralAudiencePreviewDto> PreviewAudienceAsync(
        MuralAudiencePreviewRequest request,
        CancellationToken cancellationToken = default);

    Task<MuralPostReadDto> MarkReadAsync(
        Guid muralPostId,
        Guid personId,
        Guid? userId,
        CancellationToken cancellationToken = default);

    Task<MuralReadStatsDto?> GetReadStatsAsync(
        Guid muralPostId,
        CancellationToken cancellationToken = default);
}
