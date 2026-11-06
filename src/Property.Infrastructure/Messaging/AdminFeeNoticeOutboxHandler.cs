using System.Text.Json;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Billing.BreB;
using Property.Application.Messaging;

namespace Property.Infrastructure.Messaging;

public sealed class AdminFeeNoticeOutboxHandler : IOutboxMessageHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly IAdminFeeNoticeService _notices;

    public AdminFeeNoticeOutboxHandler(IAdminFeeNoticeService notices) => _notices = notices;

    public string MessageType => OutboxMessageTypes.AdminFeeNotice;

    public Task HandleAsync(string payloadJson, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<AdminFeeNoticePayload>(payloadJson, JsonOptions)
            ?? throw new InvalidOperationException("Invalid billing.fee-notice payload.");
        return _notices.DispatchAsync(payload, cancellationToken);
    }
}
