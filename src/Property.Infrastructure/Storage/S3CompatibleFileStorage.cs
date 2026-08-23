using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Options;

namespace Property.Infrastructure.Storage;

/// <summary>S3-compatible storage (AWS S3, MinIO, etc.).</summary>
public sealed class S3CompatibleFileStorage : IFileStorage, IDisposable
{
    private readonly IAmazonS3 _client;
    private readonly string _bucket;
    private readonly ILogger<S3CompatibleFileStorage> _logger;

    public S3CompatibleFileStorage(IOptions<FileStorageOptions> options, ILogger<S3CompatibleFileStorage> logger)
    {
        _logger = logger;
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.S3Bucket))
            throw new InvalidOperationException("FileStorage:S3Bucket is required when Provider=S3.");

        _bucket = o.S3Bucket.Trim();
        var config = new AmazonS3Config
        {
            ForcePathStyle = o.S3ForcePathStyle,
            RegionEndpoint = RegionEndpoint.GetBySystemName(o.S3Region),
        };
        if (!string.IsNullOrWhiteSpace(o.S3ServiceUrl))
            config.ServiceURL = o.S3ServiceUrl.Trim();

        if (!string.IsNullOrWhiteSpace(o.S3AccessKey) && !string.IsNullOrWhiteSpace(o.S3SecretKey))
        {
            _client = new AmazonS3Client(
                new BasicAWSCredentials(o.S3AccessKey, o.S3SecretKey),
                config);
        }
        else
        {
            _client = new AmazonS3Client(config);
        }
    }

    public async Task PutAsync(
        string storageKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = storageKey,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
        };
        await _client.PutObjectAsync(request, cancellationToken);
        _logger.LogDebug("Stored S3 object {Key}", storageKey);
    }

    public async Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.GetObjectAsync(_bucket, storageKey, cancellationToken);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        await _client.DeleteObjectAsync(_bucket, storageKey, cancellationToken);
    }

    public void Dispose() => _client.Dispose();
}
