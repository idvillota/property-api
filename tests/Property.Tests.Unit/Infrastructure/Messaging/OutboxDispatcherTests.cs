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
using Property.Tests.Unit.Support;

namespace Property.Tests.Unit.Infrastructure.Messaging;

public sealed class OutboxDispatcherTests
{
    private static OutboxWriter CreateWriter(TenantDbFixture fx, int maxAttempts = 5)
    {
        var options = Options.Create(new OutboxOptions
        {
            DefaultMaxAttempts = maxAttempts,
            RetryBaseDelaySeconds = 1,
        });
        return new OutboxWriter(fx.Db, fx.TenantContext, options);
    }

    private static OutboxDispatcher CreateDispatcher(
        TenantDbFixture fx,
        IOutboxMessageHandler handler,
        int maxAttempts = 5)
    {
        var options = Options.Create(new OutboxOptions
        {
            DefaultMaxAttempts = maxAttempts,
            RetryBaseDelaySeconds = 0,
        });
        var logger = Mock.Of<ILogger<OutboxDispatcher>>();
        return new OutboxDispatcher(fx.Db, fx.TenantContext, [handler], options, logger);
    }

    private static async Task SeedTenantAsync(TenantDbFixture fx)
    {
        fx.Db.Tenants.Add(new Tenant
        {
            Id = fx.TenantId,
            Name = "Demo",
            Slug = "demo-" + fx.TenantId.ToString("N")[..8],
        });
        await fx.Db.SaveChangesAsync();
    }

    [Fact]
    public async Task Outbox_Write_InSameTransaction()
    {
        using var fx = new TenantDbFixture();
        await SeedTenantAsync(fx);

        var person = new Person
        {
            Id = Guid.NewGuid(),
            DocumentType = DocumentType.Cc,
            DocumentNumber = "1",
            FirstName = "Ana",
            LastName = "Pérez",
            Email = "ana@example.com",
            IsActive = true,
        };
        fx.Db.People.Add(person);

        var writer = CreateWriter(fx);
        await writer.EnqueueAsync(
            OutboxMessageTypes.EmailSend,
            new EmailSendPayload
            {
                ToEmail = "ana@example.com",
                Subject = "Hola",
                HtmlBody = "<p>Hi</p>",
            });

        Assert.Contains(fx.Db.ChangeTracker.Entries<Person>(), e => e.State == EntityState.Added);
        Assert.Contains(fx.Db.ChangeTracker.Entries<OutboxMessage>(), e => e.State == EntityState.Added);

        await fx.Db.SaveChangesAsync();

        Assert.Single(await fx.Db.People.ToListAsync());
        var messages = await fx.Db.OutboxMessages.ToListAsync();
        Assert.Single(messages);
        Assert.Equal(OutboxMessageStatus.Pending, messages[0].Status);
        Assert.Equal(OutboxMessageTypes.EmailSend, messages[0].MessageType);
        Assert.Equal(fx.TenantId, messages[0].TenantId);
        Assert.All(
            fx.Db.ChangeTracker.Entries().Where(e => e.Entity is Person or OutboxMessage),
            e => Assert.Equal(EntityState.Unchanged, e.State));
    }

    [Fact]
    public async Task Outbox_Worker_Retries_ThenDeadLetters()
    {
        using var fx = new TenantDbFixture();
        await SeedTenantAsync(fx);

        var writer = CreateWriter(fx, maxAttempts: 3);
        await writer.EnqueueAsync(
            OutboxMessageTypes.EmailSend,
            new EmailSendPayload { ToEmail = "x@y.com", Subject = "s", HtmlBody = "b" });
        await fx.Db.SaveChangesAsync();

        var failing = new Mock<IOutboxMessageHandler>();
        failing.SetupGet(h => h.MessageType).Returns(OutboxMessageTypes.EmailSend);
        failing.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        var dispatcher = CreateDispatcher(fx, failing.Object, maxAttempts: 3);

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
        Assert.Contains("smtp down", final.LastError);

        // AC3: Processed (or DeadLetter) is not re-sent
        failing.Invocations.Clear();
        await dispatcher.DispatchPendingAsync(10);
        failing.Verify(
            h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Outbox_Worker_SetsTenantContext()
    {
        using var fx = new TenantDbFixture();
        await SeedTenantAsync(fx);

        Guid? seenTenant = null;
        var capturing = new Mock<IOutboxMessageHandler>();
        capturing.SetupGet(h => h.MessageType).Returns(OutboxMessageTypes.EmailSend);
        capturing.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => seenTenant = fx.TenantContext.TenantId)
            .Returns(Task.CompletedTask);

        var writer = CreateWriter(fx);
        await writer.EnqueueAsync(
            OutboxMessageTypes.EmailSend,
            new EmailSendPayload { ToEmail = "a@b.com", Subject = "s", HtmlBody = "b" });
        await fx.Db.SaveChangesAsync();

        // Simulate worker starting with null tenant (would otherwise bypass filters).
        fx.TenantContext.TenantId = null;

        var dispatcher = CreateDispatcher(fx, capturing.Object);
        await dispatcher.DispatchPendingAsync(10);

        Assert.Equal(fx.TenantId, seenTenant);
        var processed = await fx.Db.OutboxMessages.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(OutboxMessageStatus.Processed, processed.Status);

        // After processing, context restored (null for worker poller).
        Assert.Null(fx.TenantContext.TenantId);
    }
}
