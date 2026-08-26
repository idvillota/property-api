using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Property.Application.Common.Interfaces;
using Property.Domain.Common;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Authorization;
using Property.Infrastructure.Common;

namespace Property.Infrastructure.Persistence.Seeding;

/// <summary>
/// Idempotent demo extras for modules added after the base seed (holding, portal roles,
/// accounting/assemblies/visits flags, WhatsApp, asambleas, encuestas, parking, webhooks).
/// Safe to run on every local API start.
/// </summary>
public static class DevelopmentExtendedDemoSeeder
{
    public static async Task EnsureAsync(
        ApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IChartOfAccountsService? chartOfAccounts,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var tenantId = DevelopmentSeedIds.TenantId;
        var exists = await db.Tenants.IgnoreQueryFilters()
            .AnyAsync(t => t.Id == tenantId, cancellationToken);
        if (!exists)
            return;

        var utcNow = DateTime.UtcNow;
        var passwordHash = passwordHasher.Hash(DevelopmentSeedIds.DefaultPassword);

        await EnsureModulesAsync(db, tenantId, cancellationToken);
        await EnsureExtraRolesAndUsersAsync(db, tenantId, passwordHash, utcNow, cancellationToken);
        await EnsureSecondPropertyAsync(db, tenantId, utcNow, cancellationToken);
        await EnsureWhatsAppOptInAsync(db, tenantId, cancellationToken);
        await EnsureOperationalSamplesAsync(db, tenantId, utcNow, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        if (chartOfAccounts is not null)
        {
            try
            {
                await chartOfAccounts.EnsureSeededAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not seed chart of accounts for demo tenant.");
            }
        }

        logger.LogInformation(
            "Extended demo ready. Users: {Admin}, {Manager}, {Porter}, {Contador}, {Owner} / {Password} (slug {Slug})",
            DevelopmentSeedIds.AdminEmail,
            DevelopmentSeedIds.ManagerEmail,
            DevelopmentSeedIds.ReceptionistEmail,
            DevelopmentSeedIds.AccountantEmail,
            DevelopmentSeedIds.OwnerEmail,
            DevelopmentSeedIds.DefaultPassword,
            DevelopmentSeedIds.TenantSlug);
    }

    private static async Task EnsureModulesAsync(
        ApplicationDbContext db,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var settings = await db.TenantSettings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);
        if (settings is null)
            return;

        settings.ModulePortalEnabled = true;
        settings.ModuleDigitalPaymentsEnabled = true;
        settings.ModuleAccountingEnabled = true;
        settings.ModuleVisitsEnabled = true;
        settings.ModuleAssembliesEnabled = true;
        settings.EmailReceiptOnPaymentEnabled = true;
        db.TenantSettings.Update(settings);
    }

    private static async Task EnsureExtraRolesAndUsersAsync(
        ApplicationDbContext db,
        Guid tenantId,
        string passwordHash,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var receptionistRoleId = await EnsureRoleWithCatalogAsync(
            db, tenantId, DevelopmentSeedIds.ReceptionistRoleId, SystemRoles.Receptionist, utcNow, cancellationToken);
        var accountantRoleId = await EnsureRoleWithCatalogAsync(
            db, tenantId, DevelopmentSeedIds.AccountantRoleId, SystemRoles.Accountant, utcNow, cancellationToken);
        var ownerRoleId = await EnsureRoleWithCatalogAsync(
            db, tenantId, DevelopmentSeedIds.OwnerRoleId, SystemRoles.Owner, utcNow, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await EnsureUserWithRoleAsync(
            db,
            tenantId,
            DevelopmentSeedIds.ReceptionistUserId,
            DevelopmentSeedIds.ReceptionistTenantUserId,
            receptionistRoleId,
            DevelopmentSeedIds.ReceptionistEmail,
            "Portería Demo",
            passwordHash,
            utcNow,
            cancellationToken);

        await EnsureUserWithRoleAsync(
            db,
            tenantId,
            DevelopmentSeedIds.AccountantUserId,
            DevelopmentSeedIds.AccountantTenantUserId,
            accountantRoleId,
            DevelopmentSeedIds.AccountantEmail,
            "Contador Demo",
            passwordHash,
            utcNow,
            cancellationToken);

        await EnsureUserWithRoleAsync(
            db,
            tenantId,
            DevelopmentSeedIds.OwnerUserId,
            DevelopmentSeedIds.OwnerTenantUserId,
            ownerRoleId,
            DevelopmentSeedIds.OwnerEmail,
            "Propietario Demo",
            passwordHash,
            utcNow,
            cancellationToken);

        // Link owner user → first seeded person (stable id from base seeder).
        var personExists = await db.People.IgnoreQueryFilters()
            .AnyAsync(p => p.Id == DevelopmentSeedIds.OwnerPersonId, cancellationToken);
        if (personExists)
        {
            var linkExists = await db.UserPersonLinks.IgnoreQueryFilters()
                .AnyAsync(l => l.Id == DevelopmentSeedIds.OwnerUserPersonLinkId, cancellationToken);
            if (!linkExists)
            {
                await db.UserPersonLinks.AddAsync(
                    new UserPersonLink
                    {
                        Id = DevelopmentSeedIds.OwnerUserPersonLinkId,
                        TenantId = tenantId,
                        UserId = DevelopmentSeedIds.OwnerUserId,
                        PersonId = DevelopmentSeedIds.OwnerPersonId,
                        IsActive = true,
                        CreatedAtUtc = utcNow,
                    },
                    cancellationToken);
            }
        }
    }

    private static async Task<Guid> EnsureRoleWithCatalogAsync(
        ApplicationDbContext db,
        Guid tenantId,
        Guid preferredRoleId,
        string roleName,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var role = await db.Roles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                r => r.TenantId == tenantId && r.Name == roleName,
                cancellationToken);
        if (role is null)
        {
            role = await db.Roles.IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Id == preferredRoleId, cancellationToken);
        }

        if (role is null)
        {
            role = new Role
            {
                Id = preferredRoleId,
                TenantId = tenantId,
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant(),
                CreatedAtUtc = utcNow,
            };
            await db.Roles.AddAsync(role, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!FeatureCatalog.DefaultFeaturesByRole.TryGetValue(roleName, out var codes))
            return role.Id;

        var features = await db.Features.AsNoTracking().ToListAsync(cancellationToken);
        var byCode = features.ToDictionary(f => f.Code, StringComparer.Ordinal);
        var existing = await db.RoleFeatures.IgnoreQueryFilters()
            .Where(rf => rf.RoleId == role.Id)
            .Select(rf => rf.FeatureId)
            .ToListAsync(cancellationToken);
        var existingSet = existing.ToHashSet();

        foreach (var code in codes)
        {
            if (!byCode.TryGetValue(code, out var feature))
                continue;
            if (existingSet.Contains(feature.Id))
                continue;
            await db.RoleFeatures.AddAsync(
                new RoleFeature
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    RoleId = role.Id,
                    FeatureId = feature.Id,
                    CreatedAtUtc = utcNow,
                },
                cancellationToken);
        }

        return role.Id;
    }

    private static async Task EnsureUserWithRoleAsync(
        ApplicationDbContext db,
        Guid tenantId,
        Guid userId,
        Guid tenantUserId,
        Guid roleId,
        string email,
        string displayName,
        string passwordHash,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var normalized = email.ToUpperInvariant();
        var user = await db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId || u.NormalizedEmail == normalized, cancellationToken);
        if (user is null)
        {
            user = new User
            {
                Id = userId,
                Email = email,
                NormalizedEmail = normalized,
                PasswordHash = passwordHash,
                DisplayName = displayName,
                CreatedAtUtc = utcNow,
            };
            await db.Users.AddAsync(user, cancellationToken);
        }
        else
        {
            user.PasswordHash = passwordHash;
            db.Users.Update(user);
        }

        var tu = await db.TenantUsers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantUserId || (t.TenantId == tenantId && t.UserId == user.Id), cancellationToken);
        if (tu is null)
        {
            tu = new TenantUser
            {
                Id = tenantUserId,
                TenantId = tenantId,
                UserId = user.Id,
                IsActive = true,
                BrandTheme = UserPreferences.DefaultBrandTheme,
                ColorScheme = UserPreferences.DefaultColorScheme,
                CreatedAtUtc = utcNow,
            };
            await db.TenantUsers.AddAsync(tu, cancellationToken);
        }

        var hasRole = await db.TenantUserRoles.IgnoreQueryFilters()
            .AnyAsync(tur => tur.TenantUserId == tu.Id && tur.RoleId == roleId, cancellationToken);
        if (!hasRole)
        {
            await db.TenantUserRoles.AddAsync(
                new TenantUserRole
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    TenantUserId = tu.Id,
                    RoleId = roleId,
                    CreatedAtUtc = utcNow,
                },
                cancellationToken);
        }
    }

    private static async Task EnsureSecondPropertyAsync(
        ApplicationDbContext db,
        Guid tenantId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (await db.HorizontalProperties.IgnoreQueryFilters()
                .AnyAsync(p => p.Id == DevelopmentSeedIds.PropertyBId, cancellationToken))
            return;

        await db.HorizontalProperties.AddAsync(
            new HorizontalProperty
            {
                Id = DevelopmentSeedIds.PropertyBId,
                TenantId = tenantId,
                Name = "Torre Norte (Holding)",
                TradeName = "Torre Norte",
                LegalName = "Copropiedad Torre Norte",
                Nit = "900111222-3",
                TaxRegime = "Régimen común",
                LegalRepresentative = "Demo Administrator",
                AddressLine = "Carrera 33 # 18-10",
                City = "Pasto",
                Country = "Colombia",
                PostalCode = "520002",
                Phone = "+57 300 000 0002",
                NeighborhoodId = DevelopmentSeedIds.NeighborhoodMaridiazId,
                PropertyUseType = PropertyUseType.Residential,
                IsActive = true,
                CreatedAtUtc = utcNow,
            },
            cancellationToken);

        await db.Buildings.AddAsync(
            new Building
            {
                Id = DevelopmentSeedIds.BuildingBId,
                TenantId = tenantId,
                HorizontalPropertyId = DevelopmentSeedIds.PropertyBId,
                Name = "Torre A",
                Code = "TN-A",
                SortOrder = 1,
                IsActive = true,
                CreatedAtUtc = utcNow,
            },
            cancellationToken);

        await db.Floors.AddAsync(
            new Floor
            {
                Id = DevelopmentSeedIds.FloorBId,
                TenantId = tenantId,
                BuildingId = DevelopmentSeedIds.BuildingBId,
                Name = "Piso 1",
                Number = 1,
                SortOrder = 1,
                IsActive = true,
                CreatedAtUtc = utcNow,
            },
            cancellationToken);

        await db.Units.AddAsync(
            new Unit
            {
                Id = DevelopmentSeedIds.UnitB1Id,
                TenantId = tenantId,
                HorizontalPropertyId = DevelopmentSeedIds.PropertyBId,
                BuildingId = DevelopmentSeedIds.BuildingBId,
                FloorId = DevelopmentSeedIds.FloorBId,
                Code = "TN-101",
                UnitType = UnitType.Apartment,
                PrivateBuiltAreaM2 = 72m,
                Coefficient = 1m,
                IsBillable = true,
                IsActive = true,
                CreatedAtUtc = utcNow,
            },
            cancellationToken);
    }

    private static async Task EnsureWhatsAppOptInAsync(
        ApplicationDbContext db,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var people = await db.People.IgnoreQueryFilters()
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .OrderBy(p => p.DocumentNumber)
            .Take(4)
            .ToListAsync(cancellationToken);
        foreach (var person in people)
        {
            person.WhatsAppOptIn = true;
            if (string.IsNullOrWhiteSpace(person.Phone))
                person.Phone = "+57 310 555 0001";
            db.People.Update(person);
        }
    }

    private static async Task EnsureOperationalSamplesAsync(
        ApplicationDbContext db,
        Guid tenantId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var propertyId = DevelopmentSeedIds.PropertyId;

        if (!await db.DunningRules.IgnoreQueryFilters()
                .AnyAsync(r => r.Id == DevelopmentSeedIds.DunningRuleId, cancellationToken))
        {
            await db.DunningRules.AddAsync(
                new DunningRule
                {
                    Id = DevelopmentSeedIds.DunningRuleId,
                    TenantId = tenantId,
                    HorizontalPropertyId = propertyId,
                    DaysOverdue = 5,
                    IsEnabled = true,
                    CooldownDays = 7,
                    CreatedAtUtc = utcNow,
                },
                cancellationToken);
        }

        if (!await db.VisitorParkingSettings.IgnoreQueryFilters()
                .AnyAsync(s => s.Id == DevelopmentSeedIds.VisitorParkingSettingsId, cancellationToken))
        {
            await db.VisitorParkingSettings.AddAsync(
                new VisitorParkingSettings
                {
                    Id = DevelopmentSeedIds.VisitorParkingSettingsId,
                    TenantId = tenantId,
                    HorizontalPropertyId = propertyId,
                    TotalSpots = 12,
                    HourlyRate = 3_000m,
                    DailyCap = 25_000m,
                    IsActive = true,
                    CreatedAtUtc = utcNow,
                },
                cancellationToken);
        }

        if (!await db.Polls.IgnoreQueryFilters()
                .AnyAsync(p => p.Id == DevelopmentSeedIds.PollId, cancellationToken))
        {
            await db.Polls.AddAsync(
                new Poll
                {
                    Id = DevelopmentSeedIds.PollId,
                    TenantId = tenantId,
                    HorizontalPropertyId = propertyId,
                    Title = "Horario de zona BBQ",
                    Description = "Encuesta demo para portal residentes.",
                    Status = PollStatus.Open,
                    VoteScope = PollVoteScope.PerUnit,
                    ShowPartialResults = true,
                    CreatedAtUtc = utcNow,
                    Options =
                    [
                        new PollOption
                        {
                            Id = Guid.Parse("a1000060-0060-4060-8060-000000000060"),
                            TenantId = tenantId,
                            PollId = DevelopmentSeedIds.PollId,
                            Label = "Hasta 9:00 p. m.",
                            SortOrder = 1,
                            CreatedAtUtc = utcNow,
                        },
                        new PollOption
                        {
                            Id = Guid.Parse("a1000061-0061-4061-8061-000000000061"),
                            TenantId = tenantId,
                            PollId = DevelopmentSeedIds.PollId,
                            Label = "Hasta 10:00 p. m.",
                            SortOrder = 2,
                            CreatedAtUtc = utcNow,
                        },
                    ],
                },
                cancellationToken);
        }

        if (!await db.Assemblies.IgnoreQueryFilters()
                .AnyAsync(a => a.Id == DevelopmentSeedIds.AssemblyId, cancellationToken))
        {
            await db.Assemblies.AddAsync(
                new Assembly
                {
                    Id = DevelopmentSeedIds.AssemblyId,
                    TenantId = tenantId,
                    HorizontalPropertyId = propertyId,
                    Title = "Asamblea ordinaria demo 2026",
                    ScheduledAtUtc = utcNow.AddDays(14),
                    Location = "Salón social",
                    Status = AssemblyStatus.Published,
                    PublishedAtUtc = utcNow,
                    QuorumPercentRequired = 50m,
                    CreatedAtUtc = utcNow,
                    AgendaItems =
                    [
                        new AgendaItem
                        {
                            Id = Guid.Parse("a1000062-0062-4062-8062-000000000062"),
                            TenantId = tenantId,
                            AssemblyId = DevelopmentSeedIds.AssemblyId,
                            SortOrder = 1,
                            Title = "Informe de administración",
                            CreatedAtUtc = utcNow,
                        },
                        new AgendaItem
                        {
                            Id = Guid.Parse("a1000063-0063-4063-8063-000000000063"),
                            TenantId = tenantId,
                            AssemblyId = DevelopmentSeedIds.AssemblyId,
                            SortOrder = 2,
                            Title = "Aprobación de presupuesto",
                            CreatedAtUtc = utcNow,
                        },
                    ],
                },
                cancellationToken);

            await db.AssemblyMotions.AddAsync(
                new AssemblyMotion
                {
                    Id = DevelopmentSeedIds.AssemblyMotionId,
                    TenantId = tenantId,
                    AssemblyId = DevelopmentSeedIds.AssemblyId,
                    Title = "Aprobar presupuesto ordinario",
                    Description = "Moción abierta para voto desde portal.",
                    VoteMode = AssemblyVoteMode.Coefficient,
                    Status = AssemblyMotionStatus.Open,
                    CreatedAtUtc = utcNow,
                },
                cancellationToken);
        }

        if (!await db.FileAssets.IgnoreQueryFilters()
                .AnyAsync(f => f.Id == DevelopmentSeedIds.LibraryFileAssetId, cancellationToken))
        {
            await db.FileAssets.AddAsync(
                new FileAsset
                {
                    Id = DevelopmentSeedIds.LibraryFileAssetId,
                    TenantId = tenantId,
                    StorageKey = $"{tenantId:N}/demo/reglamento-placeholder.pdf",
                    FileName = "reglamento-demo.pdf",
                    ContentType = "application/pdf",
                    SizeBytes = 1024,
                    Purpose = "library",
                    IsActive = true,
                    CreatedAtUtc = utcNow,
                },
                cancellationToken);
        }

        if (!await db.LibraryDocuments.IgnoreQueryFilters()
                .AnyAsync(d => d.Id == DevelopmentSeedIds.LibraryDocumentId, cancellationToken))
        {
            await db.LibraryDocuments.AddAsync(
                new LibraryDocument
                {
                    Id = DevelopmentSeedIds.LibraryDocumentId,
                    TenantId = tenantId,
                    HorizontalPropertyId = propertyId,
                    Title = "Reglamento de propiedad horizontal (demo)",
                    Category = "Reglamento",
                    FileAssetId = DevelopmentSeedIds.LibraryFileAssetId,
                    Visibility = LibraryVisibility.Owners,
                    IsActive = true,
                    CreatedAtUtc = utcNow,
                },
                cancellationToken);
        }

        if (!await db.WebhookSubscriptions.IgnoreQueryFilters()
                .AnyAsync(w => w.Id == DevelopmentSeedIds.WebhookSubscriptionId, cancellationToken))
        {
            await db.WebhookSubscriptions.AddAsync(
                new WebhookSubscription
                {
                    Id = DevelopmentSeedIds.WebhookSubscriptionId,
                    TenantId = tenantId,
                    Name = "Partner demo",
                    TargetUrl = "https://example.com/hooks/property-demo",
                    Secret = "demo-webhook-secret",
                    EventTypes = "PaymentPosted,VisitApproved,PqrsCreated",
                    IsActive = false,
                    CreatedAtUtc = utcNow,
                },
                cancellationToken);
        }

        if (!await db.MuralPosts.IgnoreQueryFilters()
                .AnyAsync(m => m.Id == DevelopmentSeedIds.MuralPostId, cancellationToken))
        {
            await db.MuralPosts.AddAsync(
                new MuralPost
                {
                    Id = DevelopmentSeedIds.MuralPostId,
                    TenantId = tenantId,
                    HorizontalPropertyId = propertyId,
                    Title = "Bienvenida al portal demo",
                    Body = "Este aviso es de prueba. Revise estado de cuenta, reservas y visitas.",
                    AuthorDisplayName = "Administración",
                    IsActive = true,
                    PublishedAtUtc = utcNow,
                    CreatedAtUtc = utcNow,
                },
                cancellationToken);
        }
    }
}
