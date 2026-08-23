using System.Text.Json;
using Property.Application.Common.Interfaces;
using Property.Application.Messaging;

namespace Property.Infrastructure.Messaging;

public sealed class WhatsAppSendOutboxHandler : IOutboxMessageHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly IWhatsAppSender _whatsAppSender;

    public WhatsAppSendOutboxHandler(IWhatsAppSender whatsAppSender) =>
        _whatsAppSender = whatsAppSender;

    public string MessageType => OutboxMessageTypes.WhatsAppSend;

    public async Task HandleAsync(string payloadJson, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<WhatsAppSendPayload>(payloadJson, JsonOptions)
            ?? throw new InvalidOperationException("Invalid whatsapp.send payload.");

        if (string.IsNullOrWhiteSpace(payload.ToPhone))
            throw new InvalidOperationException("whatsapp.send payload missing ToPhone.");

        await _whatsAppSender.SendAsync(
            payload.ToPhone.Trim(),
            payload.Body ?? string.Empty,
            payload.TemplateKey,
            cancellationToken);
    }
}
