using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Options;

namespace Property.Infrastructure.Storage;

public sealed class LocalDiskFileStorage : IFileStorage
{
    private readonly string _root;
    private readonly ILogger<LocalDiskFileStorage> _logger;

    public LocalDiskFileStorage(
        IOptions<FileStorageOptions> options,
        IHostEnvironment env,
        ILogger<LocalDiskFileStorage> logger)
    {
        _logger = logger;
        var configured = options.Value.LocalRootPath;
        _root = Path.IsPathRooted(configured)
            ? configured
            : Path.GetFullPath(Path.Combine(env.ContentRootPath, configured));
        Directory.CreateDirectory(_root);
    }

    /// <summary>Test helper — fixed root without IHostEnvironment.</summary>
    public LocalDiskFileStorage(string rootPath, ILogger<LocalDiskFileStorage>? logger = null)
    {
        _root = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(_root);
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<LocalDiskFileStorage>.Instance;
    }

    public async Task PutAsync(
        string storageKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fs, cancellationToken);
        _logger.LogDebug("Stored file {Key} ({ContentType})", storageKey, contentType);
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        if (!File.Exists(path))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        var normalized = storageKey.Replace('\\', '/').TrimStart('/');
        if (normalized.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid storage key.");

        var full = Path.GetFullPath(Path.Combine(_root, normalized.Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(_root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid storage key.");
        return full;
    }
}
