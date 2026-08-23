using Property.Application.Features.Platform.Files;

namespace Property.Application.Common.Interfaces;

public interface IPaymentReceiptService
{
    /// <summary>
    /// Generates PDF, stores FileAsset, sets Payment.ReceiptFileAssetId, optionally emails (BIL-013).
    /// Idempotent when receipt already exists.
    /// </summary>
    Task<Guid> EnsureReceiptAsync(
        Guid paymentId,
        bool enqueueEmailIfEnabled = true,
        CancellationToken cancellationToken = default);

    Task<FileDownloadResult?> DownloadReceiptAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default);
}

/// <summary>No-op receipts for unit tests that do not assert BIL-013.</summary>
public sealed class NullPaymentReceiptService : IPaymentReceiptService
{
    public Task<Guid> EnsureReceiptAsync(
        Guid paymentId,
        bool enqueueEmailIfEnabled = true,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Guid.Empty);

    public Task<FileDownloadResult?> DownloadReceiptAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<FileDownloadResult?>(null);
}
