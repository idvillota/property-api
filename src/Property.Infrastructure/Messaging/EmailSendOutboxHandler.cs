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

    public EmailSendOutboxHandler(IEmailSender emailSender) => _emailSender = emailSender;

    public string MessageType => OutboxMessageTypes.EmailSend;

    public async Task HandleAsync(string payloadJson, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<EmailSendPayload>(payloadJson, JsonOptions)
            ?? throw new InvalidOperationException("Invalid email.send payload.");

        if (string.IsNullOrWhiteSpace(payload.ToEmail))
            throw new InvalidOperationException("email.send payload missing ToEmail.");

        await _emailSender.SendAsync(
            payload.ToEmail,
            payload.Subject ?? string.Empty,
            payload.HtmlBody ?? string.Empty,
            cancellationToken);
    }
}
