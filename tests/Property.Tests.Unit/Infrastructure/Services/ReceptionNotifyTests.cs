using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Property.Application.Common.Options;
using Property.Application.Features.Reception.ReceptionItems;
using Property.Application.Messaging;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Messaging;
using Property.Infrastructure.Services;
using Property.Tests.Unit.Support;
using DomainUnit = Property.Domain.Entities.Unit;

namespace Property.Tests.Unit.Infrastructure.Services;

public sealed class ReceptionNotifyTests
{
    private static ReceptionItemService CreateSut(TenantDbFixture fx)
    {
        var outbox = new OutboxWriter(
            fx.Db,
            fx.TenantContext,
            Microsoft.Extensions.Options.Options.Create(new OutboxOptions()));
        var notifications = new ReceptionNotificationService(
            fx.Repository<ReceptionItem>(),
            fx.Repository<Person>(),
            fx.Repository<DomainUnit>(),
            fx.Repository<UnitOccupancy>(),
            fx.Repository<HorizontalProperty>(),
            outbox,
            NullLogger<ReceptionNotificationService>.Instance);

        return new ReceptionItemService(
            fx.Repository<ReceptionItem>(),
            fx.Repository<HorizontalProperty>(),
            fx.Repository<DomainUnit>(),
            fx.Repository<Person>(),
            fx.Repository<UnitOccupancy>(),
            fx.Repository<Employee>(),
            fx.UnitOfWork,
            notifications);
    }

    private static async Task<(HorizontalProperty Property, DomainUnit Unit, Person Person)> SeedAsync(
        TenantDbFixture fx)
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
            Coefficient = 1,
            IsActive = true,
            IsBillable = true,
        };
        var person = new Person
        {
            Id = Guid.NewGuid(),
            DocumentType = DocumentType.Cc,
            DocumentNumber = "1",
            FirstName = "Ana",
            LastName = "R",
            Email = "ana@example.com",
            IsActive = true,
        };
        fx.Db.HorizontalProperties.Add(property);
        fx.Db.Units.Add(unit);
        fx.Db.People.Add(person);
        fx.Db.UnitOccupancies.Add(new UnitOccupancy
        {
            Id = Guid.NewGuid(),
            UnitId = unit.Id,
            PersonId = person.Id,
            Role = OccupancyRole.Owner,
            IsCommunicationResponsible = true,
            IsActive = true,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
        });
        await fx.Db.SaveChangesAsync();
        return (property, unit, person);
    }

    [Fact]
    public async Task Reception_Notify_EnqueuesEmail()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var sut = CreateSut(fx);

        await sut.CreateAsync(new CreateReceptionItemDto
        {
            HorizontalPropertyId = seed.Property.Id,
            UnitId = seed.Unit.Id,
            PersonId = seed.Person.Id,
            Direction = ReceptionDirection.Inbound,
            ItemType = ReceptionItemType.Package,
            Notify = true,
        });

        var msg = await fx.Db.OutboxMessages.SingleAsync();
        Assert.Equal(OutboxMessageTypes.EmailSend, msg.MessageType);
        Assert.Contains("ana@example.com", msg.PayloadJson);
    }

    [Fact]
    public async Task Reception_NotifyOff_Skips()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var sut = CreateSut(fx);

        await sut.CreateAsync(new CreateReceptionItemDto
        {
            HorizontalPropertyId = seed.Property.Id,
            UnitId = seed.Unit.Id,
            PersonId = seed.Person.Id,
            Direction = ReceptionDirection.Inbound,
            ItemType = ReceptionItemType.Package,
            Notify = false,
        });

        Assert.Empty(await fx.Db.OutboxMessages.ToListAsync());
    }
}
