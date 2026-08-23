namespace Property.Application.Common.Options;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>Local | S3</summary>
    public string Provider { get; set; } = "Local";

    public string LocalRootPath { get; set; } = "App_Data/files";

    /// <summary>Max upload size in bytes (default 10 MiB).</summary>
    public long MaxBytes { get; set; } = 10 * 1024 * 1024;

    public string? S3Bucket { get; set; }
    public string? S3ServiceUrl { get; set; }
    public string? S3AccessKey { get; set; }
    public string? S3SecretKey { get; set; }
    public string S3Region { get; set; } = "us-east-1";
    public bool S3ForcePathStyle { get; set; } = true;
}
