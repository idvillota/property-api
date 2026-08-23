using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Models;
using Property.Application.Common.Options;
using Property.Application.Features.Community.Pqrs;
using Property.Application.Features.Organization.UserPersonLinks;
using Property.Application.Messaging;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Messaging;
using Property.Infrastructure.Services;
using Property.Tests.Unit.Support;
using DomainUnit = Property.Domain.Entities.Unit;

namespace Property.Tests.Unit.Infrastructure.Services;

public sealed class PqrsServiceTests
{
    private static PqrsService CreateSut(TenantDbFixture fx) =>
        new(
            fx.Db,
            fx.TenantContext,
            fx.UnitOfWork,
            new OutboxWriter(
                fx.Db,
                fx.TenantContext,
                Microsoft.Extensions.Options.Options.Create(new OutboxOptions())));

    private static async Task<(
        HorizontalProperty Property,
        DomainUnit Unit,
        Person Person,
        Person Other)> SeedAsync(TenantDbFixture fx)
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
        var other = new Person
        {
            Id = Guid.NewGuid(),
            DocumentType = DocumentType.Cc,
            DocumentNumber = "2",
            FirstName = "Bob",
            LastName = "O",
            IsActive = true,
        };

        fx.Db.HorizontalProperties.Add(property);
        fx.Db.Units.Add(unit);
        fx.Db.People.AddRange(person, other);
        await fx.Db.SaveChangesAsync();
        return (property, unit, person, other);
    }

    [Fact]
    public async Task Pqrs_Create_ForLinkedUnit()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var sut = CreateSut(fx);

        var created = await sut.CreateAsync(new CreatePqrsCaseDto
        {
            HorizontalPropertyId = seed.Property.Id,
            UnitId = seed.Unit.Id,
            CreatedByPersonId = seed.Person.Id,
            Category = PqrsCategory.Complaint,
            Subject = "Ruido",
            Description = "Hay ruido excesivo en las noches.",
        });

        Assert.Equal(PqrsStatus.Open, created.Status);
        Assert.Equal(seed.Person.Id, created.CreatedByPersonId);

        var mine = await sut.ListMineAsync(seed.Person.Id);
        Assert.Contains(mine, c => c.Id == created.Id);
    }

    [Fact]
    public async Task Pqrs_Create_Forbidden_WithoutLink()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "x@y.com",
            NormalizedEmail = "X@Y.COM",
            PasswordHash = "x",
        };
        fx.Db.Users.Add(user);
        fx.Db.TenantUsers.Add(new TenantUser { Id = Guid.NewGuid(), UserId = user.Id, IsActive = true });
        await fx.Db.SaveChangesAsync();

        var links = new UserPersonLinkService(fx.UnitOfWork, fx.Db, fx.TenantContext);
        await links.LinkAsync(seed.Person.Id, new LinkUserToPersonDto { UserId = user.Id });

        var portal = PortalServiceTests_CreatePortal(fx);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            portal.CreatePqrsAsync(
                user.Id,
                new CreatePortalPqrsDto
                {
                    UnitId = Guid.NewGuid(),
                    Category = PqrsCategory.Petition,
                    Subject = "Sin unidad",
                    Description = "No debería crearse.",
                }));
    }

    [Fact]
    public async Task Pqrs_Transition_NotifiesCreator()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var sut = CreateSut(fx);

        var created = await sut.CreateAsync(new CreatePqrsCaseDto
        {
            HorizontalPropertyId = seed.Property.Id,
            UnitId = seed.Unit.Id,
            CreatedByPersonId = seed.Person.Id,
            Category = PqrsCategory.Claim,
            Subject = "Filtración",
            Description = "Hay una filtración en el techo.",
        });

        var updated = await sut.TransitionStatusAsync(
            created.Id,
            PqrsStatus.InProgress,
            comment: "Estamos revisando.");

        Assert.NotNull(updated);
        Assert.Equal(PqrsStatus.InProgress, updated!.Status);
        Assert.Contains(updated.Comments, c => c.Body.Contains("revisando"));

        var msg = await fx.Db.OutboxMessages.SingleAsync();
        Assert.Equal(OutboxMessageTypes.EmailSend, msg.MessageType);
        Assert.Contains("ana@example.com", msg.PayloadJson);
        Assert.Contains("En progreso", msg.PayloadJson);
    }

    [Fact]
    public async Task Pqrs_List_FilterByStatus()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var sut = CreateSut(fx);

        var open = await sut.CreateAsync(new CreatePqrsCaseDto
        {
            HorizontalPropertyId = seed.Property.Id,
            UnitId = seed.Unit.Id,
            CreatedByPersonId = seed.Person.Id,
            Category = PqrsCategory.Suggestion,
            Subject = "Abierto",
            Description = "Caso abierto.",
        });
        var progress = await sut.CreateAsync(new CreatePqrsCaseDto
        {
            HorizontalPropertyId = seed.Property.Id,
            UnitId = seed.Unit.Id,
            CreatedByPersonId = seed.Person.Id,
            Category = PqrsCategory.Suggestion,
            Subject = "Progreso",
            Description = "Caso en progreso.",
        });
        await sut.TransitionStatusAsync(progress.Id, PqrsStatus.InProgress);

        var filtered = await sut.ListAsync(new ListQuery
        {
            Page = 1,
            PageSize = 50,
            Filters = new Dictionary<string, string>
            {
                ["horizontalPropertyId"] = seed.Property.Id.ToString(),
                ["status"] = nameof(PqrsStatus.Open),
            },
        });

        Assert.Single(filtered.Items);
        Assert.Equal(open.Id, filtered.Items[0].Id);
    }

    /// <summary>Local helper to avoid circular visibility issues with PortalServiceTests private CreateSut.</summary>
    private static PortalService PortalServiceTests_CreatePortal(TenantDbFixture fx)
    {
        var payments = new PaymentService(
            fx.Db,
            fx.Repository<Payment>(),
            fx.Repository<FeeCharge>(),
            fx.Repository<DomainUnit>(),
            fx.Repository<HorizontalProperty>(),
            fx.Repository<Person>(),
            fx.UnitOfWork,
            new Property.Infrastructure.Auditing.AuditWriter(fx.Db, fx.TenantContext),
            new OutboxWriter(
                fx.Db,
                fx.TenantContext,
                Microsoft.Extensions.Options.Options.Create(
                    new Property.Application.Common.Options.OutboxOptions())));
        var intents = new PaymentIntentService(
            fx.Db,
            payments,
            new Property.Application.Common.Interfaces.NullPaymentReceiptService(),
            fx.UnitOfWork,
            new OutboxWriter(
                fx.Db,
                fx.TenantContext,
                Microsoft.Extensions.Options.Options.Create(new OutboxOptions())));
        var checkout = new Property.Infrastructure.Payments.WompiPspCheckoutService(
            Microsoft.Extensions.Options.Options.Create(
                new Property.Application.Common.Options.WompiOptions
                {
                    UseMockCheckout = true,
                    PortalPublicBaseUrl = "http://localhost:5173",
                }));
        var mural = new MuralPostService(
            fx.Repository<MuralPost>(),
            fx.Repository<MuralPostRead>(),
            fx.Repository<HorizontalProperty>(),
            fx.Repository<DomainUnit>(),
            fx.Repository<Building>(),
            fx.Repository<Floor>(),
            fx.Repository<UnitOccupancy>(),
            fx.Repository<User>(),
            fx.TenantContext,
            fx.UnitOfWork);
        var pqrs = CreateSut(fx);
        var visits = new VisitLogService(
            fx.Db,
            fx.TenantContext,
            fx.UnitOfWork,
            new OutboxWriter(
                fx.Db,
                fx.TenantContext,
                Microsoft.Extensions.Options.Options.Create(new OutboxOptions())),
            new Property.Infrastructure.Auditing.AuditWriter(fx.Db, fx.TenantContext));

        return new PortalService(
            fx.Db,
            new Property.Infrastructure.Services.UserPersonLinkService(fx.UnitOfWork, fx.Db, fx.TenantContext),
            new ReservationService(
                fx.Repository<Reservation>(),
                fx.Repository<CommonArea>(),
                fx.Repository<DomainUnit>(),
                fx.Repository<Person>(),
                fx.Repository<HorizontalProperty>(),
                fx.UnitOfWork),
            intents,
            checkout,
            mural,
            pqrs,
            visits);
    }
}
