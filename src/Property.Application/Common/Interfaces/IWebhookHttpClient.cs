namespace Property.Application.Common.Interfaces;

public interface IWebhookHttpClient
{
    Task<WebhookHttpResponse> PostAsync(
        string targetUrl,
        string bodyJson,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken = default);
}

public sealed class WebhookHttpResponse
{
    public int StatusCode { get; init; }
    public string? Body { get; init; }
}
