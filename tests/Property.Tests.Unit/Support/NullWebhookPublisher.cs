using Property.Application.Common.Interfaces;

namespace Property.Tests.Unit.Support;

public sealed class NullWebhookPublisher : IWebhookPublisher
{
    public Task EnqueueEventAsync(
        string eventType,
        object payload,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
