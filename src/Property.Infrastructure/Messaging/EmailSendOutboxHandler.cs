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

        IReadOnlyList<EmailAttachment>? attachments = null;
        if (payload.AttachmentFileAssetId is Guid fileId)
        {
            var download = await _files.DownloadAsync(fileId, cancellationToken)
                ?? throw new InvalidOperationException($"Attachment file {fileId} was not found.");
            await using (download.Content)
            {
                await using var ms = new MemoryStream();
                await download.Content.CopyToAsync(ms, cancellationToken);
                attachments =
                [
                    new EmailAttachment
                    {
                        FileName = download.Asset.FileName,
                        ContentType = download.Asset.ContentType,
                        Content = ms.ToArray(),
                    },
                ];
            }
        }

        await _emailSender.SendAsync(
            payload.ToEmail,
            payload.Subject ?? string.Empty,
            payload.HtmlBody ?? string.Empty,
            attachments,
            cancellationToken);
    }
}
