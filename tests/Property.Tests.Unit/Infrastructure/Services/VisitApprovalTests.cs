using Microsoft.EntityFrameworkCore;
using Property.Application.Auditing;
using Property.Application.Common.Options;
using Property.Application.Features.Reception.Visits;
using Property.Application.Messaging;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Auditing;
using Property.Infrastructure.Messaging;
using Property.Infrastructure.Services;
using Property.Tests.Unit.Support;
using DomainUnit = Property.Domain.Entities.Unit;

namespace Property.Tests.Unit.Infrastructure.Services;

public sealed class VisitApprovalTests
{
    private static VisitLogService CreateSut(TenantDbFixture fx) =>
        new(
            fx.Db,
            fx.TenantContext,
            fx.UnitOfWork,
            new OutboxWriter(fx.Db, fx.TenantContext, Microsoft.Extensions.Options.Options.Create(new OutboxOptions())),
            new AuditWriter(fx.Db, fx.TenantContext),
            new NullWebhookPublisher());

    private static async Task<(HorizontalProperty Property, DomainUnit Unit, Person Occupant)> SeedAsync(
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
            DocumentNumber = "900100",
            FirstName = "Residente",
            LastName = "Uno",
            Email = "residente@example.com",
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
            Role = OccupancyRole.Resident,
            IsPrimary = true,
            IsActive = true,
            IsCommunicationResponsible = true,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
        });
        await fx.Db.SaveChangesAsync();
        return (property, unit, person);
    }

    [Fact]
    public async Task Visit_Approve_ByResident()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var sut = CreateSut(fx);

        var registered = await sut.RegisterAsync(new RegisterVisitDto
        {
            HorizontalPropertyId = seed.Property.Id,
            UnitId = seed.Unit.Id,
            DocumentType = DocumentType.Cc,
            DocumentNumber = "111222",
            FirstName = "Visitante",
            LastName = "X",
            RequiresApproval = true,
        });

        Assert.Equal(VisitLogStatus.PendingApproval, registered.Status);

        var approved = await sut.ApproveAsync(registered.Id, seed.Occupant.Id, Guid.NewGuid());
        Assert.Equal(VisitLogStatus.Approved, approved.Status);
        Assert.NotNull(approved.DecidedAtUtc);
    }

    [Fact]
    public async Task Visit_Expire_OnTimeout()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var sut = CreateSut(fx);

        var registered = await sut.RegisterAsync(new RegisterVisitDto
        {
            HorizontalPropertyId = seed.Property.Id,
            UnitId = seed.Unit.Id,
            DocumentType = DocumentType.Cc,
            DocumentNumber = "333444",
            FirstName = "Visitante",
            LastName = "Y",
            RequiresApproval = true,
        });

        var entity = await fx.Db.VisitLogs.SingleAsync(v => v.Id == registered.Id);
        entity.ApprovalExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
        await fx.Db.SaveChangesAsync();

        var expiredCount = await sut.ExpireOverdueAsync();
        Assert.Equal(1, expiredCount);

        var after = await sut.GetByIdAsync(registered.Id);
        Assert.Equal(VisitLogStatus.Expired, after!.Status);
    }

    [Fact]
    public async Task Visit_Override_WritesAudit()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        fx.TenantContext.UserId = Guid.NewGuid();
        var sut = CreateSut(fx);

        var registered = await sut.RegisterAsync(new RegisterVisitDto
        {
            HorizontalPropertyId = seed.Property.Id,
            UnitId = seed.Unit.Id,
            DocumentType = DocumentType.Cc,
            DocumentNumber = "555666",
            FirstName = "Visitante",
            LastName = "Z",
            RequiresApproval = true,
        });

        var result = await sut.OverrideApproveAsync(
            registered.Id,
            fx.TenantContext.UserId!.Value,
            "Residente no contesta");

        Assert.Equal(VisitLogStatus.Approved, result.Status);
        Assert.Equal("Residente no contesta", result.OverrideReason);

        var evt = await fx.Db.AuditEvents.SingleAsync();
        Assert.Equal(AuditActions.VisitOverrideApproved, evt.Action);
        Assert.Equal(AuditEntityTypes.VisitLog, evt.EntityType);
        Assert.Equal(registered.Id.ToString(), evt.EntityId);
    }
}
