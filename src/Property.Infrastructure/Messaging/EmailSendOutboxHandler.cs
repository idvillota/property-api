using System.Text.Json;
using Property.Application.Common.Interfaces;
using Property.Application.Messaging;

namespace Property.Infrastructure.Messaging;

public sealed class EmailSendOutboxHandler : IOutboxMessageHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly IEmailSender _emailSender;
    private readonly IFileAssetService _files;

    public EmailSendOutboxHandler(IEmailSender emailSender, IFileAssetService files)
    {
        _emailSender = emailSender;
        _files = files;
    }

    public string MessageType => OutboxMessageTypes.EmailSend;

    public async Task HandleAsync(string payloadJson, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<EmailSendPayload>(payloadJson, JsonOptions)
            ?? throw new InvalidOperationException("Invalid email.send payload.");

        if (string.IsNullOrWhiteSpace(payload.ToEmail))
            throw new InvalidOperationException("email.send payload missing ToEmail.");

        var attachments = new List<EmailAttachment>();
        if (payload.AttachmentFileAssetId is Guid fileId)
        {
            var download = await _files.DownloadAsync(fileId, cancellationToken)
                ?? throw new InvalidOperationException($"Attachment file {fileId} was not found.");
            await using (download.Content)
            {
                await using var ms = new MemoryStream();
                await download.Content.CopyToAsync(ms, cancellationToken);
                attachments.Add(new EmailAttachment
                {
                    FileName = download.Asset.FileName,
                    ContentType = download.Asset.ContentType,
                    Content = ms.ToArray(),
                });
            }
        }

        if (!string.IsNullOrWhiteSpace(payload.InlineContentBase64))
        {
            byte[] inline;
            try
            {
                inline = Convert.FromBase64String(payload.InlineContentBase64);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("email.send inline image is not valid base64.", ex);
            }

            attachments.Add(new EmailAttachment
            {
                FileName = string.IsNullOrWhiteSpace(payload.InlineFileName) ? "qr.png" : payload.InlineFileName,
                ContentType = string.IsNullOrWhiteSpace(payload.InlineContentType)
                    ? "image/png"
                    : payload.InlineContentType,
                Content = inline,
                ContentId = payload.InlineContentId,
            });
        }

        await _emailSender.SendAsync(
            payload.ToEmail,
            payload.Subject ?? string.Empty,
            payload.HtmlBody ?? string.Empty,
            attachments.Count == 0 ? null : attachments,
            cancellationToken);
    }
}
