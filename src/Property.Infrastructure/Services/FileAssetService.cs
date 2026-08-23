using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Options;
using Property.Application.Features.Platform.Files;
using Property.Domain.Entities;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class FileAssetService : IFileAssetService
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly ICurrentTenantContext _tenant;
    private readonly IUnitOfWork _unitOfWork;
    private readonly FileStorageOptions _options;

    public FileAssetService(
        ApplicationDbContext db,
        IFileStorage storage,
        ICurrentTenantContext tenant,
        IUnitOfWork unitOfWork,
        IOptions<FileStorageOptions> options)
    {
        _db = db;
        _storage = storage;
        _tenant = tenant;
        _unitOfWork = unitOfWork;
        _options = options.Value;
    }

    public async Task<FileAssetDto> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        long? declaredSizeBytes,
        string? purpose,
        CancellationToken cancellationToken = default)
    {
        if (_tenant.TenantId is not Guid tenantId || tenantId == Guid.Empty)
            throw new InvalidOperationException("Tenant context is required.");

        var safeName = SanitizeFileName(fileName);
        if (safeName.Length == 0)
            throw new InvalidOperationException("File name is required.");

        var size = declaredSizeBytes ?? GuessLength(content);
        if (size > _options.MaxBytes)
            throw new InvalidOperationException(
                $"File exceeds the maximum allowed size of {_options.MaxBytes} bytes.");

        // Buffer so we can measure unknown-length streams and reject oversize before persist.
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length > _options.MaxBytes)
            throw new InvalidOperationException(
                $"File exceeds the maximum allowed size of {_options.MaxBytes} bytes.");

        buffer.Position = 0;
        var storageKey = BuildStorageKey(tenantId, safeName);
        var mime = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();

        await _storage.PutAsync(storageKey, buffer, mime, cancellationToken);

        var asset = new FileAsset
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StorageKey = storageKey,
            FileName = safeName,
            ContentType = mime,
            SizeBytes = buffer.Length,
            CreatedByUserId = _tenant.UserId,
            Purpose = string.IsNullOrWhiteSpace(purpose) ? null : purpose.Trim(),
            IsActive = true,
        };

        await _db.FileAssets.AddAsync(asset, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(asset);
    }

    public async Task<FileAssetDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var asset = await _db.FileAssets.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id && f.IsActive, cancellationToken);
        return asset is null ? null : Map(asset);
    }

    public async Task<FileDownloadResult?> DownloadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var asset = await _db.FileAssets.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id && f.IsActive, cancellationToken);
        if (asset is null)
            return null;

        var stream = await _storage.OpenReadAsync(asset.StorageKey, cancellationToken);
        if (stream is null)
            return null;

        return new FileDownloadResult
        {
            Asset = Map(asset),
            Content = stream,
        };
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var asset = await _db.FileAssets.FirstOrDefaultAsync(f => f.Id == id && f.IsActive, cancellationToken);
        if (asset is null)
            return false;

        asset.IsActive = false;
        _db.FileAssets.Update(asset);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string BuildStorageKey(Guid tenantId, string safeName)
    {
        var stamp = DateTime.UtcNow.ToString("yyyy/MM");
        return $"{tenantId:N}/{stamp}/{Guid.NewGuid():N}_{safeName}";
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName.Trim());
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var sb = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            if (char.IsAsciiLetterOrDigit(ch) || ch is '.' or '-' or '_' or ' ')
                sb.Append(ch == ' ' ? '_' : ch);
        }

        var cleaned = sb.ToString().Trim('_');
        return cleaned.Length == 0 ? "file" : cleaned[..Math.Min(cleaned.Length, 180)];
    }

    private static long GuessLength(Stream content)
    {
        try
        {
            if (content.CanSeek)
                return content.Length;
        }
        catch
        {
            // ignore
        }

        return 0;
    }

    private static FileAssetDto Map(FileAsset entity) =>
        new()
        {
            Id = entity.Id,
            FileName = entity.FileName,
            ContentType = entity.ContentType,
            SizeBytes = entity.SizeBytes,
            Purpose = entity.Purpose,
            CreatedByUserId = entity.CreatedByUserId,
            CreatedAtUtc = entity.CreatedAtUtc,
        };
}
