using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Platform.Files;
using Property.Application.Messaging;
using Property.Domain.Enums;
using Property.Infrastructure.Billing;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class PaymentReceiptService : IPaymentReceiptService
{
    private readonly ApplicationDbContext _db;
    private readonly IFileAssetService _files;
    private readonly IOutboxWriter _outbox;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentReceiptService(
        ApplicationDbContext db,
        IFileAssetService files,
        IOutboxWriter outbox,
        IUnitOfWork unitOfWork)
    {
        _db = db;
        _files = files;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> EnsureReceiptAsync(
        Guid paymentId,
        bool enqueueEmailIfEnabled = true,
        CancellationToken cancellationToken = default)
    {
        var payment = await _db.Payments
            .Include(p => p.Unit)
            .Include(p => p.HorizontalProperty)
            .Include(p => p.Person)
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Payment was not found.");

        if (payment.ReceiptFileAssetId is Guid existing)
        {
            if (enqueueEmailIfEnabled)
                await TryEnqueueEmailAsync(payment, existing, cancellationToken);
            return existing;
        }

        var unitCode = payment.Unit?.Code ?? payment.UnitId.ToString("N")[..8];
        var propertyName = payment.HorizontalProperty?.Name ?? "Conjunto";
        var pdf = ReceiptPdfBuilder.Build(
            propertyName,
            unitCode,
            payment.Amount,
            payment.PaidAt,
            payment.Reference,
            MethodLabel(payment.Method),
            payment.Id);

        await using var stream = new MemoryStream(pdf);
        var asset = await _files.UploadAsync(
            stream,
            $"recibo-{payment.Id:N}.pdf",
            "application/pdf",
            pdf.Length,
            "receipt",
            cancellationToken);

        payment.ReceiptFileAssetId = asset.Id;
        _db.Payments.Update(payment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (enqueueEmailIfEnabled)
            await TryEnqueueEmailAsync(payment, asset.Id, cancellationToken);

        return asset.Id;
    }

    public async Task<FileDownloadResult?> DownloadReceiptAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        var payment = await _db.Payments.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.IsActive, cancellationToken);
        if (payment is null)
            return null;

        if (payment.ReceiptFileAssetId is null)
            await EnsureReceiptAsync(paymentId, enqueueEmailIfEnabled: false, cancellationToken);

        payment = await _db.Payments.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);
        if (payment?.ReceiptFileAssetId is not Guid fileId)
            return null;

        return await _files.DownloadAsync(fileId, cancellationToken);
    }

    private async Task TryEnqueueEmailAsync(
        Domain.Entities.Payment payment,
        Guid fileAssetId,
        CancellationToken cancellationToken)
    {
        var settings = await _db.TenantSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (settings is not null && !settings.EmailReceiptOnPaymentEnabled)
            return;

        string? email = payment.Person?.Email;
        if (string.IsNullOrWhiteSpace(email) && payment.PersonId is null)
        {
            email = await _db.UnitOccupancies.AsNoTracking()
                .Where(o => o.UnitId == payment.UnitId && o.IsActive && o.IsCommunicationResponsible)
                .Join(_db.People.AsNoTracking(), o => o.PersonId, p => p.Id, (_, p) => p.Email)
                .FirstOrDefaultAsync(e => e != null && e != "", cancellationToken);
        }
        else if (string.IsNullOrWhiteSpace(email) && payment.PersonId is Guid personId)
        {
            email = await _db.People.AsNoTracking()
                .Where(p => p.Id == personId)
                .Select(p => p.Email)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(email))
            return;

        var unitCode = payment.Unit?.Code ?? "";
        await _outbox.EnqueueAsync(
            OutboxMessageTypes.EmailSend,
            new EmailSendPayload
            {
                ToEmail = email!,
                Subject = $"Recibo de caja — unidad {unitCode}",
                HtmlBody =
                    $"""
                    <p>Adjuntamos el recibo de caja por <strong>{payment.Amount:0.00}</strong>
                    para la unidad <strong>{System.Net.WebUtility.HtmlEncode(unitCode)}</strong>.</p>
                    """,
                AttachmentFileAssetId = fileAssetId,
            },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string MethodLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Efectivo",
        PaymentMethod.Transfer => "Transferencia",
        PaymentMethod.Check => "Cheque",
        PaymentMethod.Pse => "PSE",
        PaymentMethod.Other => "Otro",
        _ => method.ToString(),
    };
}
