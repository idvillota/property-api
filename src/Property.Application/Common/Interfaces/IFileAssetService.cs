using Property.Application.Features.Platform.Files;

namespace Property.Application.Common.Interfaces;

public interface IFileAssetService
{
    Task<FileAssetDto> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        long? declaredSizeBytes,
        string? purpose,
        CancellationToken cancellationToken = default);

    Task<FileAssetDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FileDownloadResult?> DownloadAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
