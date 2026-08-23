using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Options;
using Property.Application.Messaging;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Messaging;
using Property.Infrastructure.Services;
using Property.Tests.Unit.Support;

namespace Property.Tests.Unit.Infrastructure.Messaging;

public sealed class WebhookDeliverTests
{
    private sealed class FakeWebhookHttpClient : IWebhookHttpClient
    {
        public int StatusCode { get; set; } = 200;
        public string? LastBody { get; private set; }
        public string? LastSignature { get; private set; }
        public int CallCount { get; private set; }

        public Task<WebhookHttpResponse> PostAsync(
            string targetUrl,
            string bodyJson,
            IReadOnlyDictionary<string, string> headers,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastBody = bodyJson;
            LastSignature = headers.TryGetValue("X-Webhook-Signature", out var sig) ? sig : null;
            return Task.FromResult(new WebhookHttpResponse { StatusCode = StatusCode });
        }
    }

    [Fact]
    public async Task Webhook_SignedPayload()
    {
        var fake = new FakeWebhookHttpClient();
        var handler = new WebhookDeliverOutboxHandler(fake);

        const string secret = "test-secret";
        const string body = """{"paymentId":"abc"}""";
        var payload = JsonSerializer.Serialize(new WebhookDeliverPayload
        {
            SubscriptionId = Guid.NewGuid(),
            TargetUrl = "https://partner.example/hook",
            Secret = secret,
            EventType = WebhookEventTypes.PaymentPosted,
            BodyJson = body,
        });

        await handler.HandleAsync(payload);

        Assert.Equal(body, fake.LastBody);
        var expected = WebhookDeliverOutboxHandler.ComputeSignature(body, secret);
        Assert.Equal(expected, fake.LastSignature);
    }

    [Fact]
    public async Task Webhook_Retry_On5xx()
    {
        using var fx = new TenantDbFixture();
        fx.Db.Tenants.Add(new Tenant
        {
            Id = fx.TenantId,
            Name = "Demo",
            Slug = "demo-" + fx.TenantId.ToString("N")[..8],
        });
        await fx.Db.SaveChangesAsync();

        var fake = new FakeWebhookHttpClient { StatusCode = 503 };
        var handler = new WebhookDeliverOutboxHandler(fake);
        var dispatcher = new OutboxDispatcher(
            fx.Db,
            fx.TenantContext,
            [handler],
            Options.Create(new OutboxOptions { DefaultMaxAttempts = 3, RetryBaseDelaySeconds = 0 }),
            Mock.Of<ILogger<OutboxDispatcher>>());

        var writer = new OutboxWriter(
            fx.Db,
            fx.TenantContext,
            Options.Create(new OutboxOptions { DefaultMaxAttempts = 3 }));

        await writer.EnqueueAsync(
            OutboxMessageTypes.WebhookDeliver,
            new WebhookDeliverPayload
            {
                SubscriptionId = Guid.NewGuid(),
                TargetUrl = "https://partner.example/hook",
                Secret = "s",
                EventType = WebhookEventTypes.PaymentPosted,
                BodyJson = """{"ok":true}""",
            });
        await fx.Db.SaveChangesAsync();

        for (var i = 0; i < 3; i++)
        {
            var msg = await fx.Db.OutboxMessages.IgnoreQueryFilters().SingleAsync();
            msg.NextAttemptUtc = DateTime.UtcNow.AddMinutes(-1);
            msg.Status = OutboxMessageStatus.Pending;
            await fx.Db.SaveChangesAsync();
            await dispatcher.DispatchPendingAsync(10);
        }

        var final = await fx.Db.OutboxMessages.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(OutboxMessageStatus.DeadLetter, final.Status);
        Assert.Equal(3, final.AttemptCount);
        Assert.Equal(3, fake.CallCount);
    }

    [Fact]
    public async Task WebhookPublisher_EnqueuesForMatchingSubscription()
    {
        using var fx = new TenantDbFixture();
        fx.Db.WebhookSubscriptions.Add(new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            Name = "Partner",
            TargetUrl = "https://partner.example/hook",
            Secret = "secret",
            EventTypes = $"{WebhookEventTypes.PaymentPosted},{WebhookEventTypes.PqrsCreated}",
            IsActive = true,
        });
        await fx.Db.SaveChangesAsync();

        var outbox = new OutboxWriter(
            fx.Db,
            fx.TenantContext,
            Options.Create(new OutboxOptions()));
        var publisher = new WebhookPublisher(fx.Db, outbox);

        await publisher.EnqueueEventAsync(
            WebhookEventTypes.PaymentPosted,
            new { paymentId = Guid.NewGuid() });
        await fx.Db.SaveChangesAsync();

        var msg = Assert.Single(await fx.Db.OutboxMessages.ToListAsync());
        Assert.Equal(OutboxMessageTypes.WebhookDeliver, msg.MessageType);
        Assert.Contains("paymentId", msg.PayloadJson, StringComparison.OrdinalIgnoreCase);
    }
}
