using Microsoft.EntityFrameworkCore;
using Property.Application.Features.Reception.Visits;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Services;
using Property.Tests.Unit.Support;
using DomainUnit = Property.Domain.Entities.Unit;

namespace Property.Tests.Unit.Infrastructure.Services;

public sealed class VisitLogServiceTests
{
    private sealed record Seed(HorizontalProperty Property, DomainUnit Unit);

    private static async Task<Seed> SeedPropertyAsync(TenantDbFixture fx)
    {
        var property = new HorizontalProperty
        {
            Id = Guid.NewGuid(),
            Name = "PH Demo",
            AddressLine = "Calle 1",
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
        fx.Db.HorizontalProperties.Add(property);
        fx.Db.Units.Add(unit);
        await fx.Db.SaveChangesAsync();
        return new Seed(property, unit);
    }

    private static VisitLogService CreateSut(TenantDbFixture fx) =>
        new(
            fx.Db,
            fx.TenantContext,
            fx.UnitOfWork,
            new Property.Infrastructure.Messaging.OutboxWriter(
                fx.Db,
                fx.TenantContext,
                Microsoft.Extensions.Options.Options.Create(
                    new Property.Application.Common.Options.OutboxOptions())),
            new Property.Infrastructure.Auditing.AuditWriter(fx.Db, fx.TenantContext),
            new NullWebhookPublisher());

    private static RegisterVisitDto Dto(Seed seed, string doc = "12345678", string first = "Ana") =>
        new()
        {
            HorizontalPropertyId = seed.Property.Id,
            UnitId = seed.Unit.Id,
            DocumentType = DocumentType.Cc,
            DocumentNumber = doc,
            FirstName = first,
            LastName = "Pérez",
            RequiresApproval = false,
        };

    [Fact]
    public async Task Visit_Register_CreatesLog()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedPropertyAsync(fx);
        var sut = CreateSut(fx);

        var result = await sut.RegisterAsync(Dto(seed));

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(VisitLogStatus.Registered, result.Status);
        Assert.Equal(seed.Unit.Id, result.UnitId);
        Assert.False(result.VisitorWasReused);
        Assert.Equal(1, await fx.Db.People.CountAsync());
        Assert.Equal(1, await fx.Db.VisitLogs.CountAsync());
    }

    [Fact]
    public async Task Visit_Register_ReusesPersonByDocument()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedPropertyAsync(fx);
        var sut = CreateSut(fx);

        var first = await sut.RegisterAsync(Dto(seed, "99887766", "Ana"));
        var second = await sut.RegisterAsync(Dto(seed, "99887766", "Ana María"));

        Assert.Equal(first.VisitorPersonId, second.VisitorPersonId);
        Assert.True(second.VisitorWasReused);
        Assert.Equal(1, await fx.Db.People.CountAsync());
        Assert.Equal(2, await fx.Db.VisitLogs.CountAsync());
    }

    [Fact]
    public async Task Visit_Register_RequiresUnit()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedPropertyAsync(fx);
        var sut = CreateSut(fx);
        var dto = Dto(seed);
        dto.UnitId = Guid.Empty;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RegisterAsync(dto));
        Assert.Contains("Unit", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await fx.Db.VisitLogs.CountAsync());
    }
}
