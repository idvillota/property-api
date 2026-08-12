using Property.Application.Features.Structure.Composer;

namespace Property.Application.Common.Interfaces;

public interface IStructureComposerService
{
    Task<StructureComposerStatusDto> GetStatusAsync(
        Guid horizontalPropertyId,
        CancellationToken cancellationToken = default);

    Task<StructureWorkspaceDto> GetWorkspaceAsync(
        Guid horizontalPropertyId,
        CancellationToken cancellationToken = default);

    Task<StructureComposerPreviewDto> PreviewAsync(
        StructureComposerRequestDto request,
        CancellationToken cancellationToken = default);

    Task<StructureComposerCommitResultDto> CommitAsync(
        StructureComposerRequestDto request,
        CancellationToken cancellationToken = default);
}
