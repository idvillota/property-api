using System.Text;
using Property.Application.Common.Interfaces;

namespace Property.Infrastructure.Services;

public sealed class HttpWebhookClient : IWebhookHttpClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HttpWebhookClient(IHttpClientFactory httpClientFactory) =>
        _httpClientFactory = httpClientFactory;

    public async Task<WebhookHttpResponse> PostAsync(
        string targetUrl,
        string bodyJson,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(nameof(HttpWebhookClient));
        using var request = new HttpRequestMessage(HttpMethod.Post, targetUrl)
        {
            Content = new StringContent(bodyJson, Encoding.UTF8, "application/json"),
        };

        foreach (var (key, value) in headers)
            request.Headers.TryAddWithoutValidation(key, value);

        using var response = await client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        return new WebhookHttpResponse
        {
            StatusCode = (int)response.StatusCode,
            Body = responseBody,
        };
    }
}
