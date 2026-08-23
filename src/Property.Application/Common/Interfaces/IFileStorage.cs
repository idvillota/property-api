namespace Property.Application.Common.Interfaces;

/// <summary>Binary blob store (local disk or S3-compatible).</summary>
public interface IFileStorage
{
    Task PutAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
