using Property.Domain.Common;

namespace Property.Domain.Entities;

/// <summary>Tenant-scoped file metadata (ARCH-004). Binary lives in IFileStorage.</summary>
public class FileAsset : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }

    /// <summary>Provider object key, e.g. {tenantId}/{yyyy}/{guid}_{safeName}.</summary>
    public string StorageKey { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/octet-stream";

    public long SizeBytes { get; set; }

    public Guid? CreatedByUserId { get; set; }

    /// <summary>Optional feature tag, e.g. pqrs, receipt, assembly-minutes.</summary>
    public string? Purpose { get; set; }

    public bool IsActive { get; set; } = true;

    public User? CreatedByUser { get; set; }
}
