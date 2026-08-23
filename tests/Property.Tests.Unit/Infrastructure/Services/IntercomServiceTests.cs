using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Options;
using Property.Application.Messaging;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Messaging;
using Property.Infrastructure.Services;
using Property.Tests.Unit.Support;
using DomainUnit = Property.Domain.Entities.Unit;

namespace Property.Tests.Unit.Infrastructure.Services;

public sealed class IntercomServiceTests
{
    private static IntercomService CreateSut(TenantDbFixture fx, IVisitIntercomRealtime? realtime = null) =>
        new(
            fx.Db,
            fx.TenantContext,
            fx.UnitOfWork,
            new OutboxWriter(fx.Db, fx.TenantContext, Microsoft.Extensions.Options.Options.Create(new OutboxOptions())),
            realtime ?? new NullVisitIntercomRealtime());

    private static async Task<(HorizontalProperty Property, DomainUnit Unit, Person Occupant, VisitLog Visit)> SeedAsync(
        TenantDbFixture fx)
    {
        var property = new HorizontalProperty
        {
            Id = Guid.NewGuid(),
            Name = "PH",
            AddressLine = "1",
            City = "Pasto",
            Country = "Colombia",
            IsActive = true,
        };
        var unit = new DomainUnit
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Code = "202",
            UnitType = UnitType.Apartment,
            Coefficient = 1m,
            IsActive = true,
            IsBillable = true,
        };
        var occupant = new Person
        {
            Id = Guid.NewGuid(),
            DocumentType = DocumentType.Cc,
            DocumentNumber = "700100",
            FirstName = "Ana",
            LastName = "Habitante",
            Email = "ana@example.com",
            IsActive = true,
        };
        var visitor = new Person
        {
            Id = Guid.NewGuid(),
            DocumentType = DocumentType.Cc,
            DocumentNumber = "800100",
            FirstName = "Visitante",
            LastName = "Intercom",
            IsActive = true,
        };
        var visit = new VisitLog
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            UnitId = unit.Id,
            VisitorPersonId = visitor.Id,
            EnteredAtUtc = DateTime.UtcNow,
            Status = VisitLogStatus.PendingApproval,
            ApprovalExpiresAtUtc = DateTime.UtcNow.AddMinutes(VisitApprovalOptions.TimeoutMinutes),
        };

        fx.Db.HorizontalProperties.Add(property);
        fx.Db.Units.Add(unit);
        fx.Db.People.AddRange(occupant, visitor);
        fx.Db.UnitOccupancies.Add(new UnitOccupancy
        {
            Id = Guid.NewGuid(),
            UnitId = unit.Id,
            PersonId = occupant.Id,
            Role = OccupancyRole.Resident,
            IsPrimary = true,
            IsActive = true,
            IsCommunicationResponsible = true,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
        });
        fx.Db.VisitLogs.Add(visit);
        await fx.Db.SaveChangesAsync();
        return (property, unit, occupant, visit);
    }

    [Fact]
    public async Task IntercomNotify_CreatesNotification()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var sut = CreateSut(fx);

        await sut.NotifyUnitAsync(seed.Visit.Id);

        var note = await fx.Db.InAppNotifications.SingleAsync();
        Assert.Equal(IntercomService.NotificationType, note.Type);
        Assert.Equal(seed.Occupant.Id, note.PersonId);
        Assert.Equal(seed.Visit.Id, note.RelatedEntityId);
        Assert.Equal("/portal/visitas", note.LinkPath);
    }

    [Fact]
    public async Task Intercom_FallbackEmail_WhenOffline()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        // Null realtime reports 0 connections → offline; service always enqueues email.
        var sut = CreateSut(fx, new NullVisitIntercomRealtime());

        await sut.NotifyUnitAsync(seed.Visit.Id);

        var email = await fx.Db.OutboxMessages.SingleAsync(m => m.MessageType == OutboxMessageTypes.EmailSend);
        Assert.Contains("ana@example.com", email.PayloadJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("porter", email.PayloadJson, StringComparison.OrdinalIgnoreCase);
    }
}
