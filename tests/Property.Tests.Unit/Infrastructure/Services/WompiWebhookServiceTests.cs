using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Property.Application.Auditing;
using Property.Application.Common.Options;
using Property.Application.Features.Billing.PaymentIntents;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Auditing;
using Property.Infrastructure.Messaging;
using Property.Infrastructure.Payments;
using Property.Infrastructure.Services;
using Property.Tests.Unit.Support;
using DomainUnit = Property.Domain.Entities.Unit;

namespace Property.Tests.Unit.Infrastructure.Services;

public sealed class WompiWebhookServiceTests
{
    private const string EventsSecret = "test_events_secret";

    private sealed record Seed(HorizontalProperty Property, DomainUnit Unit, FeeCharge Charge, PaymentIntent Intent);

    private static async Task<Seed> SeedIntentAsync(TenantDbFixture fx)
    {
        var property = new HorizontalProperty
        {
            Id = Guid.NewGuid(),
            Name = "PH",
            AddressLine = "1",
            City = "Pasto",
            Country = "CO",
            IsActive = true,
        };
        var unit = new DomainUnit
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Code = "101",
            UnitType = UnitType.Apartment,
            Coefficient = 1m,
            IsActive = true,
            IsBillable = true,
        };
        var period = new BillingPeriod
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Year = 2026,
            Month = 8,
            Status = BillingPeriodStatus.Open,
        };
        var charge = new FeeCharge
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            BillingPeriodId = period.Id,
            UnitId = unit.Id,
            ChargeType = FeeChargeType.Ordinary,
            Description = "Cuota",
            CoefficientSnapshot = 1m,
            Amount = 100_000m,
            AmountPaid = 0m,
            DueDate = new DateOnly(2026, 8, 30),
            Status = FeeChargeStatus.Pending,
            IsActive = true,
        };
        var intent = new PaymentIntent
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = $"pse-{Guid.NewGuid():N}",
            Provider = "Wompi",
            ProviderRef = $"tx-{Guid.NewGuid():N}",
            Status = PaymentIntentStatus.RequiresAction,
            HorizontalPropertyId = property.Id,
            UnitId = unit.Id,
            Amount = 100_000m,
            AllocationsJson = JsonSerializer.Serialize(new[]
            {
                new { feeChargeId = charge.Id, amount = 100_000m },
            }),
        };

        fx.Db.HorizontalProperties.Add(property);
        fx.Db.Units.Add(unit);
        fx.Db.BillingPeriods.Add(period);
        fx.Db.FeeCharges.Add(charge);
        fx.Db.PaymentIntents.Add(intent);
        await fx.Db.SaveChangesAsync();
        return new Seed(property, unit, charge, intent);
    }

    private static WompiWebhookService CreateSut(TenantDbFixture fx)
    {
        var intents = new PaymentIntentService(
            fx.Db,
            new PaymentService(
                fx.Db,
                fx.Repository<Payment>(),
                fx.Repository<FeeCharge>(),
                fx.Repository<DomainUnit>(),
                fx.Repository<HorizontalProperty>(),
                fx.Repository<Person>(),
                fx.UnitOfWork,
                new AuditWriter(fx.Db, fx.TenantContext)),
            fx.UnitOfWork,
            new OutboxWriter(fx.Db, fx.TenantContext, Options.Create(new OutboxOptions())));

        return new WompiWebhookService(
            fx.Db,
            fx.TenantContext,
            intents,
            new AuditWriter(fx.Db, fx.TenantContext),
            fx.UnitOfWork,
            Options.Create(new WompiOptions { EventsSecret = EventsSecret }),
            NullLogger<WompiWebhookService>.Instance);
    }

    private static string BuildEventJson(Seed seed, string status, out string checksum)
    {
        var txId = seed.Intent.ProviderRef!;
        var amountCents = 10000000;
        var timestamp = 1530291411L;
        var concat = $"{txId}{status}{amountCents}{timestamp}{EventsSecret}";
        checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(concat)));

        var payload = new
        {
            @event = "transaction.updated",
            data = new
            {
                transaction = new
                {
                    id = txId,
                    amount_in_cents = amountCents,
                    reference = seed.Intent.Id.ToString(),
                    status,
                    currency = "COP",
                },
            },
            signature = new
            {
                properties = new[] { "transaction.id", "transaction.status", "transaction.amount_in_cents" },
                timestamp,
                checksum,
            },
            sent_at = "2026-08-23T12:00:00.000Z",
        };
        return JsonSerializer.Serialize(payload);
    }

    [Fact]
    public async Task PspWebhook_Duplicate_DoesNotDoublePost()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedIntentAsync(fx);
        var sut = CreateSut(fx);
        var json = BuildEventJson(seed, "APPROVED", out var checksum);

        await sut.ProcessWompiEventAsync(json, checksum);
        await sut.ProcessWompiEventAsync(json, checksum);

        Assert.Equal(1, await fx.Db.Payments.CountAsync(p => p.IsActive));
        var intent = await fx.Db.PaymentIntents.SingleAsync(i => i.Id == seed.Intent.Id);
        Assert.Equal(PaymentIntentStatus.Paid, intent.Status);
        Assert.NotNull(intent.PaymentId);
    }

    [Fact]
    public async Task PspWebhook_InvalidSignature_Rejected()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedIntentAsync(fx);
        var sut = CreateSut(fx);
        var json = BuildEventJson(seed, "APPROVED", out _);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.ProcessWompiEventAsync(json, "DEADBEEF"));

        Assert.Contains("Checksum", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await fx.Db.Payments.CountAsync());
        Assert.Contains(
            await fx.Db.AuditEvents.ToListAsync(),
            e => e.Action == AuditActions.PspWebhookInvalidSignature);
    }
}
