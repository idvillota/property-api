using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Property.Application.Common.Interfaces;
using Property.Application.Messaging;

namespace Property.Infrastructure.Messaging;

public sealed class WebhookDeliverOutboxHandler : IOutboxMessageHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly IWebhookHttpClient _httpClient;

    public WebhookDeliverOutboxHandler(IWebhookHttpClient httpClient) => _httpClient = httpClient;

    public string MessageType => OutboxMessageTypes.WebhookDeliver;

    public async Task HandleAsync(string payloadJson, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<WebhookDeliverPayload>(payloadJson, JsonOptions)
            ?? throw new InvalidOperationException("Invalid webhook.deliver payload.");

        if (string.IsNullOrWhiteSpace(payload.TargetUrl))
            throw new InvalidOperationException("webhook.deliver payload missing TargetUrl.");

        var body = payload.BodyJson ?? string.Empty;
        var signature = ComputeSignature(body, payload.Secret);

        var response = await _httpClient.PostAsync(
            payload.TargetUrl,
            body,
            new Dictionary<string, string>
            {
                ["X-Webhook-Signature"] = signature,
                ["X-Webhook-Event"] = payload.EventType,
            },
            cancellationToken);

        if (response.StatusCode >= 500)
            throw new InvalidOperationException(
                $"Webhook delivery failed with status {response.StatusCode}.");
    }

    internal static string ComputeSignature(string body, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret ?? string.Empty);
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var hash = HMACSHA256.HashData(keyBytes, bodyBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
