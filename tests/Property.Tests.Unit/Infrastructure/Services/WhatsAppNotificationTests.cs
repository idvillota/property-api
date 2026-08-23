using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Property.Application.Common.Options;
using Property.Application.Messaging;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Messaging;
using Property.Infrastructure.Services;
using Property.Tests.Unit.Support;
using DomainUnit = Property.Domain.Entities.Unit;

namespace Property.Tests.Unit.Infrastructure.Services;

public sealed class WhatsAppNotificationTests
{
    private sealed record Seed(
        HorizontalProperty Property,
        DomainUnit Unit,
        Person Person,
        FeeCharge Charge,
        DunningRule Rule);

    private static async Task<Seed> SeedDunningAsync(
        TenantDbFixture fx,
        bool whatsAppOptIn = false,
        string? phone = null,
        string? email = null)
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
        var person = new Person
        {
            Id = Guid.NewGuid(),
            DocumentType = DocumentType.Cc,
            DocumentNumber = Guid.NewGuid().ToString("N")[..8],
            FirstName = "Ana",
            LastName = "Pérez",
            Email = email,
            Phone = phone,
            WhatsAppOptIn = whatsAppOptIn,
            IsActive = true,
        };
        var occupancy = new UnitOccupancy
        {
            Id = Guid.NewGuid(),
            UnitId = unit.Id,
            PersonId = person.Id,
            Role = OccupancyRole.Owner,
            IsPrimary = true,
            IsCommunicationResponsible = true,
            IsActive = true,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
        };
        var period = new BillingPeriod
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Year = 2026,
            Month = 7,
            Status = BillingPeriodStatus.Open,
        };
        var due = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-12);
        var charge = new FeeCharge
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            BillingPeriodId = period.Id,
            UnitId = unit.Id,
            ChargeType = FeeChargeType.Ordinary,
            Description = "Cuota",
            CoefficientSnapshot = 1m,
            Amount = 200_000m,
            AmountPaid = 0m,
            DueDate = due,
            Status = FeeChargeStatus.Pending,
            IsActive = true,
        };
        var rule = new DunningRule
        {
            Id = Guid.NewGuid(),
            TenantId = fx.TenantId,
            HorizontalPropertyId = property.Id,
            DaysOverdue = 5,
            IsEnabled = true,
            CooldownDays = 7,
            EmailSubjectTemplate = "Mora {{unitCode}}",
            EmailBodyTemplate = "<p>{{personName}} — {{balance}}</p>",
        };

        fx.Db.HorizontalProperties.Add(property);
        fx.Db.Units.Add(unit);
        fx.Db.People.Add(person);
        fx.Db.UnitOccupancies.Add(occupancy);
        fx.Db.BillingPeriods.Add(period);
        fx.Db.FeeCharges.Add(charge);
        fx.Db.DunningRules.Add(rule);
        await fx.Db.SaveChangesAsync();
        return new Seed(property, unit, person, charge, rule);
    }

    private static DunningJobService CreateDunningSut(TenantDbFixture fx) =>
        new(
            fx.Db,
            fx.TenantContext,
            new OutboxWriter(
                fx.Db,
                fx.TenantContext,
                Microsoft.Extensions.Options.Options.Create(new OutboxOptions())),
            fx.UnitOfWork,
            NullLogger<DunningJobService>.Instance);

    private static VisitLogService CreateVisitSut(TenantDbFixture fx) =>
        new(
            fx.Db,
            fx.TenantContext,
            fx.UnitOfWork,
            new OutboxWriter(
                fx.Db,
                fx.TenantContext,
                Microsoft.Extensions.Options.Options.Create(new OutboxOptions())),
            new Property.Infrastructure.Auditing.AuditWriter(fx.Db, fx.TenantContext),
            new NullWebhookPublisher());

    [Fact]
    public async Task WhatsAppDunning_RequiresOptIn()
    {
        using var fx = new TenantDbFixture();
        await SeedDunningAsync(fx, whatsAppOptIn: false, phone: "+573001234567", email: "ana@example.com");
        var sut = CreateDunningSut(fx);

        var count = await sut.RunAsync(DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.Equal(1, count);
        var messages = await fx.Db.OutboxMessages.ToListAsync();
        Assert.Single(messages);
        Assert.Equal(OutboxMessageTypes.EmailSend, messages[0].MessageType);
        Assert.Empty(await fx.Db.NotificationLogs.ToListAsync());
    }

    [Fact]
    public async Task WhatsAppDunning_EnqueuesWhenOptIn()
    {
        using var fx = new TenantDbFixture();
        await SeedDunningAsync(fx, whatsAppOptIn: true, phone: "+573001234567", email: "ana@example.com");
        var sut = CreateDunningSut(fx);

        var count = await sut.RunAsync(DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.Equal(1, count);
        var messages = await fx.Db.OutboxMessages.OrderBy(m => m.CreatedAtUtc).ToListAsync();
        Assert.Equal(2, messages.Count);
        Assert.Contains(messages, m => m.MessageType == OutboxMessageTypes.EmailSend);
        Assert.Contains(messages, m => m.MessageType == OutboxMessageTypes.WhatsAppSend);

        var log = Assert.Single(await fx.Db.NotificationLogs.ToListAsync());
        Assert.Equal("whatsapp", log.Channel);
        Assert.Equal("dunning", log.Purpose);
        Assert.Equal("+573001234567", log.ToAddress);
    }

    [Fact]
    public async Task WhatsAppVisit_Notify_OnPendingApproval()
    {
        using var fx = new TenantDbFixture();
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
        var occupant = new Person
        {
            Id = Guid.NewGuid(),
            DocumentType = DocumentType.Cc,
            DocumentNumber = "occ1",
            FirstName = "María",
            LastName = "López",
            Email = "maria@example.com",
            Phone = "+573009998877",
            WhatsAppOptIn = true,
            IsActive = true,
        };
        fx.Db.HorizontalProperties.Add(property);
        fx.Db.Units.Add(unit);
        fx.Db.People.Add(occupant);
        fx.Db.UnitOccupancies.Add(new UnitOccupancy
        {
            Id = Guid.NewGuid(),
            UnitId = unit.Id,
            PersonId = occupant.Id,
            Role = OccupancyRole.Owner,
            IsCommunicationResponsible = true,
            IsActive = true,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
        });
        await fx.Db.SaveChangesAsync();

        var sut = CreateVisitSut(fx);
        await sut.RegisterAsync(new Property.Application.Features.Reception.Visits.RegisterVisitDto
        {
            HorizontalPropertyId = property.Id,
            UnitId = unit.Id,
            DocumentType = DocumentType.Cc,
            DocumentNumber = "vis123",
            FirstName = "Visit",
            LastName = "Guest",
            RequiresApproval = true,
        });

        var messages = await fx.Db.OutboxMessages.ToListAsync();
        Assert.Contains(messages, m => m.MessageType == OutboxMessageTypes.EmailSend);
        Assert.Contains(messages, m => m.MessageType == OutboxMessageTypes.WhatsAppSend);

        var log = Assert.Single(await fx.Db.NotificationLogs.ToListAsync());
        Assert.Equal("whatsapp", log.Channel);
        Assert.Equal("visit", log.Purpose);
    }
}
