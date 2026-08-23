using Property.Application.Features.Webhooks;

namespace Property.Application.Common.Interfaces;

public interface IWebhookSubscriptionService
{
    Task<IReadOnlyList<WebhookSubscriptionDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<WebhookSubscriptionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<WebhookSubscriptionDto> CreateAsync(
        CreateWebhookSubscriptionDto dto,
        CancellationToken cancellationToken = default);

    Task<WebhookSubscriptionDto?> UpdateAsync(
        Guid id,
        UpdateWebhookSubscriptionDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IWebhookPublisher
{
    Task EnqueueEventAsync(
        string eventType,
        object payload,
        CancellationToken cancellationToken = default);
}
