using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Property.Application.Common.Options;
using Property.Application.Features.Assemblies;
using Property.Application.Features.Community;
using Property.Application.Features.Parking;
using Property.Application.Messaging;
using Property.Application.Features.Platform.Files;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Messaging;
using Property.Infrastructure.Services;
using Property.Infrastructure.Services.Assemblies;
using Property.Infrastructure.Services.Community;
using Property.Infrastructure.Services.Parking;
using Property.Infrastructure.Storage;
using Property.Tests.Unit.Support;
using DomainUnit = Property.Domain.Entities.Unit;

namespace Property.Tests.Unit.Infrastructure.Services;

public sealed class AssembliesParkingCommunityTests
{
    private sealed record Seed(
        HorizontalProperty Property,
        DomainUnit UnitA,
        DomainUnit UnitB,
        Person Owner,
        Person ProxyPerson);

    [Fact]
    public async Task Assembly_Publish_NotifiesOwners()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var outbox = CreateOutbox(fx);
        var svc = CreateAssemblyService(fx, outbox);

        var assembly = await svc.CreateAsync(new CreateAssemblyDto
        {
            HorizontalPropertyId = seed.Property.Id,
            Title = "Ordinaria 2026",
            ScheduledAtUtc = DateTime.UtcNow.AddDays(7),
            AgendaItems = [new CreateAgendaItemDto { SortOrder = 1, Title = "Cuotas" }],
        });

        await svc.PublishAsync(assembly.Id);

        var emails = await fx.Db.OutboxMessages
            .Where(m => m.MessageType == OutboxMessageTypes.EmailSend)
            .ToListAsync();
        Assert.NotEmpty(emails);
        Assert.Contains(emails, m => m.PayloadJson.Contains(seed.Owner.Email!));
    }

    [Fact]
    public async Task Quorum_Calculated_ByCoefficient()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var svc = CreateAssemblyService(fx, CreateOutbox(fx));

        var assembly = await svc.CreateAsync(new CreateAssemblyDto
        {
            HorizontalPropertyId = seed.Property.Id,
            Title = "Quorum test",
            ScheduledAtUtc = DateTime.UtcNow.AddDays(1),
            QuorumPercentRequired = 50m,
        });

        await svc.RegisterAttendanceAsync(new RegisterAttendanceDto
        {
            AssemblyId = assembly.Id,
            UnitId = seed.UnitA.Id,
        });

        var quorum = await svc.GetQuorumAsync(assembly.Id);
        Assert.Equal(0.6m, quorum.PresentCoefficient);
        Assert.Equal(1.0m, quorum.TotalCoefficient);
        Assert.Equal(60m, quorum.AchievedPercent);
        Assert.True(quorum.HasQuorum);
    }

    [Fact]
    public async Task Proxy_AddsCoefficient()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var svc = CreateAssemblyService(fx, CreateOutbox(fx));

        var assembly = await svc.CreateAsync(new CreateAssemblyDto
        {
            HorizontalPropertyId = seed.Property.Id,
            Title = "Proxy test",
            ScheduledAtUtc = DateTime.UtcNow.AddDays(1),
        });

        await svc.RegisterProxyAsync(new RegisterProxyDto
        {
            AssemblyId = assembly.Id,
            GrantorUnitId = seed.UnitB.Id,
            ProxyPersonId = seed.ProxyPerson.Id,
        });

        var quorum = await svc.GetQuorumAsync(assembly.Id);
        Assert.Equal(0.4m, quorum.ProxyCoefficient);
        Assert.Equal(40m, quorum.AchievedPercent);
    }

    [Fact]
    public async Task Vote_Weighted_ByCoefficient()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var svc = CreateAssemblyService(fx, CreateOutbox(fx));

        var assembly = await svc.CreateAsync(new CreateAssemblyDto
        {
            HorizontalPropertyId = seed.Property.Id,
            Title = "Vote test",
            ScheduledAtUtc = DateTime.UtcNow.AddDays(1),
        });

        var motion = await svc.CreateMotionAsync(new CreateMotionDto
        {
            AssemblyId = assembly.Id,
            Title = "Aprobar presupuesto",
            VoteMode = AssemblyVoteMode.Coefficient,
        });

        await svc.CastVoteAsync(new CastVoteDto
        {
            MotionId = motion.Id,
            UnitId = seed.UnitA.Id,
            Choice = AssemblyVoteChoice.Yes,
        });

        var result = await svc.CastVoteAsync(new CastVoteDto
        {
            MotionId = motion.Id,
            UnitId = seed.UnitB.Id,
            Choice = AssemblyVoteChoice.No,
        });

        Assert.Equal(0.6m, result.YesWeight);
        Assert.Equal(0.4m, result.NoWeight);
    }

    [Fact]
    public async Task MinutesPdf_ContainsResults()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var (svc, root) = CreateAssemblyServiceWithFiles(fx);

        var assembly = await svc.CreateAsync(new CreateAssemblyDto
        {
            HorizontalPropertyId = seed.Property.Id,
            Title = "Acta test",
            ScheduledAtUtc = DateTime.UtcNow.AddDays(1),
        });

        var motion = await svc.CreateMotionAsync(new CreateMotionDto
        {
            AssemblyId = assembly.Id,
            Title = "Mocion A",
        });

        await svc.CastVoteAsync(new CastVoteDto
        {
            MotionId = motion.Id,
            UnitId = seed.UnitA.Id,
            Choice = AssemblyVoteChoice.Yes,
        });

        var closed = await svc.GenerateMinutesAsync(assembly.Id);
        Assert.NotNull(closed.MinutesFileAssetId);

        var asset = await fx.Db.FileAssets.FirstAsync(f => f.Id == closed.MinutesFileAssetId);
        var path = Path.Combine(root, asset.StorageKey);
        var pdfText = Encoding.ASCII.GetString(await File.ReadAllBytesAsync(path));
        Assert.Contains("Mocion A", pdfText);
        Assert.Contains("SI=", pdfText);

        Directory.Delete(root, true);
    }

    [Fact]
    public async Task VisitorParking_Rejects_WhenFull()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var parking = CreateParkingService(fx);

        await parking.UpsertSettingsAsync(new UpsertVisitorParkingSettingsDto
        {
            HorizontalPropertyId = seed.Property.Id,
            TotalSpots = 1,
            HourlyRate = 3000m,
        });
        await parking.OpenShiftAsync(new OpenParkingShiftDto { HorizontalPropertyId = seed.Property.Id });
        await parking.IssueTicketAsync(new IssueParkingTicketDto
        {
            HorizontalPropertyId = seed.Property.Id,
            Plate = "ABC123",
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            parking.IssueTicketAsync(new IssueParkingTicketDto
            {
                HorizontalPropertyId = seed.Property.Id,
                Plate = "XYZ999",
            }));
    }

    [Fact]
    public void ParkingRate_AppliesHourly()
    {
        using var fx = new TenantDbFixture();
        var parking = CreateParkingService(fx);
        var entered = new DateTime(2026, 8, 23, 10, 0, 0, DateTimeKind.Utc);
        var exited = entered.AddHours(2).AddMinutes(5);
        var fee = parking.CalculateFee(5000m, entered, exited, null);
        Assert.Equal(15_000m, fee);
    }

    [Fact]
    public async Task ParkingTicket_UniqueCode()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var parking = CreateParkingService(fx);

        await parking.UpsertSettingsAsync(new UpsertVisitorParkingSettingsDto
        {
            HorizontalPropertyId = seed.Property.Id,
            TotalSpots = 5,
            HourlyRate = 3000m,
        });
        await parking.OpenShiftAsync(new OpenParkingShiftDto { HorizontalPropertyId = seed.Property.Id });

        var t1 = await parking.IssueTicketAsync(new IssueParkingTicketDto
        {
            HorizontalPropertyId = seed.Property.Id,
            Plate = "AAA111",
        });
        var t2 = await parking.IssueTicketAsync(new IssueParkingTicketDto
        {
            HorizontalPropertyId = seed.Property.Id,
            Plate = "BBB222",
        });

        Assert.NotEqual(t1.Code, t2.Code);
    }

    [Fact]
    public async Task ParkingShift_Close_LocksTotals()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var parking = CreateParkingService(fx);

        await parking.UpsertSettingsAsync(new UpsertVisitorParkingSettingsDto
        {
            HorizontalPropertyId = seed.Property.Id,
            TotalSpots = 5,
            HourlyRate = 4000m,
        });
        var shift = await parking.OpenShiftAsync(new OpenParkingShiftDto { HorizontalPropertyId = seed.Property.Id });
        var ticket = await parking.IssueTicketAsync(new IssueParkingTicketDto
        {
            HorizontalPropertyId = seed.Property.Id,
            Plate = "LOCK01",
        });
        await parking.ExitTicketAsync(new ExitParkingTicketDto { TicketId = ticket.Id });
        await parking.CloseShiftAsync(new CloseParkingShiftDto { ShiftId = shift.Id, DeclaredCash = 4000m });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            parking.ExitTicketAsync(new ExitParkingTicketDto { TicketId = ticket.Id }));
    }

    [Fact]
    public async Task ParkingFee_Calculated_OnExit()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var parking = CreateParkingService(fx);

        await parking.UpsertSettingsAsync(new UpsertVisitorParkingSettingsDto
        {
            HorizontalPropertyId = seed.Property.Id,
            TotalSpots = 5,
            HourlyRate = 2000m,
            DailyCap = 10000m,
        });
        await parking.OpenShiftAsync(new OpenParkingShiftDto { HorizontalPropertyId = seed.Property.Id });
        var ticket = await parking.IssueTicketAsync(new IssueParkingTicketDto
        {
            HorizontalPropertyId = seed.Property.Id,
            Plate = "FEE123",
        });

        var closed = await parking.ExitTicketAsync(new ExitParkingTicketDto { TicketId = ticket.Id });
        Assert.NotNull(closed.FeeAmount);
        Assert.True(closed.FeeAmount >= 2000m);
    }

    [Fact]
    public async Task Library_List_RespectsVisibility()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var (files, root) = CreateFileService(fx);
        var library = new LibraryService(fx.Db, fx.TenantContext);

        var asset = await UploadTextFileAsync(files, "reglamento.pdf");
        await library.CreateAsync(new CreateLibraryDocumentDto
        {
            HorizontalPropertyId = seed.Property.Id,
            Title = "Reglamento",
            Category = "Legal",
            FileAssetId = asset.Id,
            Visibility = LibraryVisibility.Owners,
        });
        await library.CreateAsync(new CreateLibraryDocumentDto
        {
            HorizontalPropertyId = seed.Property.Id,
            Title = "Interno",
            Category = "Admin",
            FileAssetId = asset.Id,
            Visibility = LibraryVisibility.AdminOnly,
        });

        var ownerView = await library.ListPortalAsync(seed.Property.Id, isOwner: true);
        var residentView = await library.ListPortalAsync(seed.Property.Id, isOwner: false);

        Assert.Single(ownerView);
        Assert.Equal("Reglamento", ownerView[0].Title);
        Assert.Empty(residentView);

        Directory.Delete(root, true);
    }

    [Fact]
    public async Task Library_Upload_StoresFileAsset()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var (files, root) = CreateFileService(fx);
        var library = new LibraryService(fx.Db, fx.TenantContext);

        var asset = await UploadTextFileAsync(files, "acta.pdf");
        var doc = await library.CreateAsync(new CreateLibraryDocumentDto
        {
            HorizontalPropertyId = seed.Property.Id,
            Title = "Acta 2025",
            Category = "Actas",
            FileAssetId = asset.Id,
        });

        Assert.Equal(asset.Id, doc.FileAssetId);
        Assert.True(await fx.Db.FileAssets.AnyAsync(f => f.Id == asset.Id));

        Directory.Delete(root, true);
    }

    [Fact]
    public async Task Maintenance_Complete_SchedulesNext()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var maintenance = new MaintenanceService(fx.Db, fx.TenantContext);

        var plan = await maintenance.CreatePlanAsync(new CreateMaintenancePlanDto
        {
            HorizontalPropertyId = seed.Property.Id,
            Name = "Ascensor",
            Frequency = MaintenanceFrequency.Monthly,
            FirstTaskTitle = "Revisión mensual",
            FirstDueDate = new DateOnly(2026, 8, 1),
        });

        var task = plan.Tasks[0];
        var next = await maintenance.CompleteTaskAsync(task.Id);

        Assert.True(next.DueDate > task.DueDate);
        var plans = await maintenance.ListAsync(seed.Property.Id);
        Assert.Equal(2, plans[0].Tasks.Count);
    }

    [Fact]
    public async Task Insurance_Alert_Within30Days()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var outbox = CreateOutbox(fx);
        var insurance = new InsuranceService(fx.Db, fx.TenantContext, outbox);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await insurance.CreateAsync(new CreateInsurancePolicyDto
        {
            HorizontalPropertyId = seed.Property.Id,
            InsurerName = "Seguros SA",
            PolicyNumber = "POL-001",
            StartDate = today.AddMonths(-11),
            EndDate = today.AddDays(15),
        });

        var sent = await insurance.RunExpiryAlertsAsync();
        Assert.Equal(1, sent);
        Assert.True(await fx.Db.OutboxMessages.AnyAsync(m => m.MessageType == OutboxMessageTypes.EmailSend));
    }

    [Fact]
    public async Task Insurance_RequiresEndDate()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var insurance = new InsuranceService(fx.Db, fx.TenantContext, CreateOutbox(fx));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            insurance.CreateAsync(new CreateInsurancePolicyDto
            {
                HorizontalPropertyId = seed.Property.Id,
                InsurerName = "Seguros SA",
                PolicyNumber = "POL-X",
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = null,
            }));
    }

    [Fact]
    public async Task Poll_OneVotePerUnit()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var polls = new PollService(fx.Db, fx.TenantContext);

        var poll = await polls.CreateAsync(new CreatePollDto
        {
            HorizontalPropertyId = seed.Property.Id,
            Title = "Color fachada",
            Options = ["Blanco", "Gris"],
        });
        poll = await polls.OpenAsync(poll.Id);
        var optionId = poll.Options[0].Id;

        await polls.CastVoteAsync(new CastPollVoteDto
        {
            PollId = poll.Id,
            PollOptionId = optionId,
            UnitId = seed.UnitA.Id,
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            polls.CastVoteAsync(new CastPollVoteDto
            {
                PollId = poll.Id,
                PollOptionId = poll.Options[1].Id,
                UnitId = seed.UnitA.Id,
            }));
    }

    [Fact]
    public async Task Poll_Reject_WhenClosed()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var polls = new PollService(fx.Db, fx.TenantContext);

        var poll = await polls.CreateAsync(new CreatePollDto
        {
            HorizontalPropertyId = seed.Property.Id,
            Title = "Encuesta cerrada",
            Options = ["A", "B"],
        });
        poll = await polls.OpenAsync(poll.Id);
        await polls.CloseAsync(poll.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            polls.CastVoteAsync(new CastPollVoteDto
            {
                PollId = poll.Id,
                PollOptionId = poll.Options[0].Id,
                UnitId = seed.UnitA.Id,
            }));
    }

    private static async Task<Seed> SeedAsync(TenantDbFixture fx)
    {
        var property = new HorizontalProperty
        {
            Id = Guid.NewGuid(),
            TenantId = fx.TenantId,
            Name = "PH Asambleas",
            AddressLine = "1",
            City = "Pasto",
            Country = "CO",
            IsActive = true,
        };
        var unitA = new DomainUnit
        {
            Id = Guid.NewGuid(),
            TenantId = fx.TenantId,
            HorizontalPropertyId = property.Id,
            Code = "101",
            UnitType = UnitType.Apartment,
            Coefficient = 0.6m,
            IsActive = true,
            IsBillable = true,
        };
        var unitB = new DomainUnit
        {
            Id = Guid.NewGuid(),
            TenantId = fx.TenantId,
            HorizontalPropertyId = property.Id,
            Code = "102",
            UnitType = UnitType.Apartment,
            Coefficient = 0.4m,
            IsActive = true,
            IsBillable = true,
        };
        var owner = new Person
        {
            Id = Guid.NewGuid(),
            TenantId = fx.TenantId,
            DocumentType = DocumentType.Cc,
            DocumentNumber = "100",
            FirstName = "Carlos",
            LastName = "Dueño",
            Email = "owner@test.local",
            IsActive = true,
        };
        var proxy = new Person
        {
            Id = Guid.NewGuid(),
            TenantId = fx.TenantId,
            DocumentType = DocumentType.Cc,
            DocumentNumber = "101",
            FirstName = "Ana",
            LastName = "Apoderada",
            IsActive = true,
        };
        fx.Db.HorizontalProperties.Add(property);
        fx.Db.Units.AddRange(unitA, unitB);
        fx.Db.People.AddRange(owner, proxy);
        fx.Db.UnitOccupancies.Add(new UnitOccupancy
        {
            Id = Guid.NewGuid(),
            TenantId = fx.TenantId,
            UnitId = unitA.Id,
            PersonId = owner.Id,
            Role = OccupancyRole.Owner,
            IsActive = true,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
        });
        await fx.Db.SaveChangesAsync();
        return new Seed(property, unitA, unitB, owner, proxy);
    }

    private static OutboxWriter CreateOutbox(TenantDbFixture fx) =>
        new(fx.Db, fx.TenantContext, Options.Create(new OutboxOptions()));

    private static AssemblyService CreateAssemblyService(TenantDbFixture fx, OutboxWriter outbox)
    {
        var (files, root) = CreateFileService(fx);
        var svc = new AssemblyService(fx.Db, fx.TenantContext, outbox, files);
        Directory.Delete(root, true);
        return svc;
    }

    private static (AssemblyService Service, string Root) CreateAssemblyServiceWithFiles(TenantDbFixture fx)
    {
        var (files, root) = CreateFileService(fx);
        var svc = new AssemblyService(fx.Db, fx.TenantContext, CreateOutbox(fx), files);
        return (svc, root);
    }

    private static VisitorParkingService CreateParkingService(TenantDbFixture fx) =>
        new(fx.Db, fx.TenantContext);

    private static (FileAssetService Files, string Root) CreateFileService(TenantDbFixture fx)
    {
        var root = Path.Combine(Path.GetTempPath(), "asm-parking-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var files = new FileAssetService(
            fx.Db,
            new LocalDiskFileStorage(root),
            fx.TenantContext,
            fx.UnitOfWork,
            Options.Create(new FileStorageOptions { MaxBytes = 5_000_000, Provider = "Local" }));
        return (files, root);
    }

    private static async Task<FileAssetDto> UploadTextFileAsync(FileAssetService files, string name)
    {
        var bytes = Encoding.UTF8.GetBytes("test file");
        await using var ms = new MemoryStream(bytes);
        return await files.UploadAsync(ms, name, "application/pdf", bytes.Length, "library", CancellationToken.None);
    }
}
