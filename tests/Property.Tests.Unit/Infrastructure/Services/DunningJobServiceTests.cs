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

public sealed class DunningJobServiceTests
{
    private sealed record Seed(
        HorizontalProperty Property,
        DomainUnit Unit,
        Person Person,
        FeeCharge Charge,
        DunningRule Rule);

    private static async Task<Seed> SeedAsync(
        TenantDbFixture fx,
        int daysPastDue = 10,
        bool enableRule = true,
        int cooldownDays = 7)
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
            DocumentNumber = "123",
            FirstName = "Ana",
            LastName = "Pérez",
            Email = "ana@example.com",
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
        var due = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-daysPastDue);
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
            IsEnabled = enableRule,
            CooldownDays = cooldownDays,
            EmailSubjectTemplate = "Mora {{unitCode}}",
            EmailBodyTemplate = "<p>{{personName}} — {{balance}} — {{daysOverdue}}</p>",
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

    private static DunningJobService CreateSut(TenantDbFixture fx) =>
        new(
            fx.Db,
            fx.TenantContext,
            new OutboxWriter(
                fx.Db,
                fx.TenantContext,
                Microsoft.Extensions.Options.Options.Create(new OutboxOptions())),
            fx.UnitOfWork,
            NullLogger<DunningJobService>.Instance);

    [Fact]
    public async Task DunningJob_SelectsOverdueUnits()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx, daysPastDue: 12);
        var sut = CreateSut(fx);
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        var count = await sut.RunAsync(asOf);

        Assert.Equal(1, count);
        Assert.Equal(1, await fx.Db.DunningSendLogs.CountAsync());
        var log = await fx.Db.DunningSendLogs.SingleAsync();
        Assert.Equal(seed.Unit.Id, log.UnitId);
        Assert.Equal("ana@example.com", log.ToEmail);
        Assert.True(log.DaysOverdueSnapshot >= 5);
    }

    [Fact]
    public async Task DunningJob_RespectsCooldown()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx, daysPastDue: 12, cooldownDays: 7);
        var sut = CreateSut(fx);
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        Assert.Equal(1, await sut.RunAsync(asOf));
        Assert.Equal(0, await sut.RunAsync(asOf));
        Assert.Equal(1, await fx.Db.DunningSendLogs.CountAsync(l => l.UnitId == seed.Unit.Id));
    }

    [Fact]
    public async Task DunningJob_WritesOutboxMessage()
    {
        using var fx = new TenantDbFixture();
        await SeedAsync(fx, daysPastDue: 12);
        var sut = CreateSut(fx);

        var count = await sut.RunAsync(DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.Equal(1, count);
        Assert.Equal(1, await fx.Db.OutboxMessages.CountAsync());
        var msg = await fx.Db.OutboxMessages.SingleAsync();
        Assert.Equal(OutboxMessageTypes.EmailSend, msg.MessageType);
        Assert.Contains("ana@example.com", msg.PayloadJson, StringComparison.OrdinalIgnoreCase);
    }
}
