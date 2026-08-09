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
/// Local-only demo seed: one tenant, one PH, structure/people, and ~6 months of
/// billing, reservations, and reception history. Idempotent by tenant id/slug.
/// </summary>
public static class DevelopmentDataSeeder
{
    private const decimal MonthlyBudget = 15_000_000m;
    private const int AptCount = 12;
    private const int ParkingCount = 10;

    public static async Task SeedAsync(
        ApplicationDbContext db,
        IPasswordHasher passwordHasher,
        CurrentTenantContext tenantContext,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var alreadySeeded = await db.Tenants
            .IgnoreQueryFilters()
            .AnyAsync(
                t => t.Id == DevelopmentSeedIds.TenantId || t.Slug == DevelopmentSeedIds.TenantSlug,
                cancellationToken);

        var utcNow = DateTime.UtcNow;
        var passwordHash = passwordHasher.Hash(DevelopmentSeedIds.DefaultPassword);
        tenantContext.TenantId = DevelopmentSeedIds.TenantId;

        if (alreadySeeded)
        {
            logger.LogInformation(
                "Development seed skipped (tenant {Slug} already exists).",
                DevelopmentSeedIds.TenantSlug);
            await EnsurePersonalAdminAsync(db, passwordHash, utcNow, logger, cancellationToken);
            return;
        }

        var today = DateOnly.FromDateTime(utcNow);
        var tenantId = DevelopmentSeedIds.TenantId;

        logger.LogInformation("Seeding development demo data for last 6 months…");

        await SeedPlatformAsync(db, passwordHash, utcNow, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var (floors, apartments, parkings, people) = await SeedStructureAndPeopleAsync(
            db,
            tenantId,
            utcNow,
            today,
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var billableUnits = apartments.Concat(parkings).ToList();
        await SeedBillingHistoryAsync(db, tenantId, billableUnits, people, today, utcNow, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await SeedAmenitiesAndReceptionAsync(
            db,
            tenantId,
            apartments,
            people,
            today,
            utcNow,
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await EnsurePersonalAdminAsync(db, passwordHash, utcNow, logger, cancellationToken);

        logger.LogInformation(
            "Development seed complete. Login: {Admin} / {Password} (tenant slug: {Slug}). Also: {Manager}, {Personal}",
            DevelopmentSeedIds.AdminEmail,
            DevelopmentSeedIds.DefaultPassword,
            DevelopmentSeedIds.TenantSlug,
            DevelopmentSeedIds.ManagerEmail,
            DevelopmentSeedIds.PersonalAdminEmail);
    }

    /// <summary>
    /// Ensures a personal Admin user exists on the demo tenant (safe to re-run).
    /// </summary>
    private static async Task EnsurePersonalAdminAsync(
        ApplicationDbContext db,
        string passwordHash,
        DateTime utcNow,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var email = DevelopmentSeedIds.PersonalAdminEmail;
        var normalized = email.ToUpperInvariant();

        var user = await db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Id = DevelopmentSeedIds.PersonalAdminUserId,
                Email = email,
                NormalizedEmail = normalized,
                PasswordHash = passwordHash,
                DisplayName = "Iván Villota",
                CreatedAtUtc = utcNow,
            };
            await db.Users.AddAsync(user, cancellationToken);
        }
        else
        {
            user.PasswordHash = passwordHash;
            user.DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? "Iván Villota" : user.DisplayName;
            db.Users.Update(user);
        }

        var tenantUser = await db.TenantUsers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                tu => tu.TenantId == DevelopmentSeedIds.TenantId && tu.UserId == user.Id,
                cancellationToken);

        if (tenantUser is null)
        {
            tenantUser = new TenantUser
            {
                Id = DevelopmentSeedIds.PersonalAdminTenantUserId,
                TenantId = DevelopmentSeedIds.TenantId,
                UserId = user.Id,
                IsActive = true,
                BrandTheme = UserPreferences.DefaultBrandTheme,
                ColorScheme = UserPreferences.DefaultColorScheme,
                CreatedAtUtc = utcNow,
            };
            await db.TenantUsers.AddAsync(tenantUser, cancellationToken);
        }
        else if (!tenantUser.IsActive)
        {
            tenantUser.IsActive = true;
            db.TenantUsers.Update(tenantUser);
        }

        var hasAdminRole = await db.TenantUserRoles.IgnoreQueryFilters()
            .AnyAsync(
                tur => tur.TenantUserId == tenantUser.Id && tur.RoleId == DevelopmentSeedIds.AdminRoleId,
                cancellationToken);

        if (!hasAdminRole)
        {
            await db.TenantUserRoles.AddAsync(
                new TenantUserRole
                {
                    Id = Guid.NewGuid(),
                    TenantId = DevelopmentSeedIds.TenantId,
                    TenantUserId = tenantUser.Id,
                    RoleId = DevelopmentSeedIds.AdminRoleId,
                    CreatedAtUtc = utcNow,
                },
                cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Personal admin ready: {Email} / {Password} (tenant slug: {Slug})",
            email,
            DevelopmentSeedIds.DefaultPassword,
            DevelopmentSeedIds.TenantSlug);
    }

    private static async Task SeedPlatformAsync(
        ApplicationDbContext db,
        string passwordHash,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var tenant = new Tenant
        {
            Id = DevelopmentSeedIds.TenantId,
            Name = "Demo Administradora",
            Slug = DevelopmentSeedIds.TenantSlug,
            TimeZoneId = "America/Bogota",
            CurrencyCode = "COP",
            IsActive = true,
            CreatedAtUtc = utcNow,
        };

        var adminUser = new User
        {
            Id = DevelopmentSeedIds.AdminUserId,
            Email = DevelopmentSeedIds.AdminEmail,
            NormalizedEmail = DevelopmentSeedIds.AdminEmail.ToUpperInvariant(),
            PasswordHash = passwordHash,
            DisplayName = "Demo Administrator",
            CreatedAtUtc = utcNow,
        };

        var managerUser = new User
        {
            Id = DevelopmentSeedIds.ManagerUserId,
            Email = DevelopmentSeedIds.ManagerEmail,
            NormalizedEmail = DevelopmentSeedIds.ManagerEmail.ToUpperInvariant(),
            PasswordHash = passwordHash,
            DisplayName = "Demo Manager",
            CreatedAtUtc = utcNow,
        };

        var adminRole = new Role
        {
            Id = DevelopmentSeedIds.AdminRoleId,
            TenantId = tenant.Id,
            Name = SystemRoles.Administrator,
            NormalizedName = SystemRoles.Administrator.ToUpperInvariant(),
            CreatedAtUtc = utcNow,
        };

        var managerRole = new Role
        {
            Id = DevelopmentSeedIds.ManagerRoleId,
            TenantId = tenant.Id,
            Name = SystemRoles.Manager,
            NormalizedName = SystemRoles.Manager.ToUpperInvariant(),
            CreatedAtUtc = utcNow,
        };

        var adminTu = new TenantUser
        {
            Id = DevelopmentSeedIds.AdminTenantUserId,
            TenantId = tenant.Id,
            UserId = adminUser.Id,
            IsActive = true,
            BrandTheme = UserPreferences.DefaultBrandTheme,
            ColorScheme = UserPreferences.DefaultColorScheme,
            CreatedAtUtc = utcNow,
        };

        var managerTu = new TenantUser
        {
            Id = DevelopmentSeedIds.ManagerTenantUserId,
            TenantId = tenant.Id,
            UserId = managerUser.Id,
            IsActive = true,
            BrandTheme = UserPreferences.DefaultBrandTheme,
            ColorScheme = UserPreferences.DefaultColorScheme,
            CreatedAtUtc = utcNow,
        };

        await db.Tenants.AddAsync(tenant, cancellationToken);
        await db.Users.AddRangeAsync([adminUser, managerUser], cancellationToken);
        await db.Roles.AddRangeAsync([adminRole, managerRole], cancellationToken);
        await db.TenantUsers.AddRangeAsync([adminTu, managerTu], cancellationToken);

        await db.TenantUserRoles.AddRangeAsync(
            [
                new TenantUserRole
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    TenantUserId = adminTu.Id,
                    RoleId = adminRole.Id,
                    CreatedAtUtc = utcNow,
                },
                new TenantUserRole
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    TenantUserId = managerTu.Id,
                    RoleId = managerRole.Id,
                    CreatedAtUtc = utcNow,
                },
            ],
            cancellationToken);

        var features = await db.Features.AsNoTracking().ToListAsync(cancellationToken);
        foreach (var feature in features)
        {
            await db.RoleFeatures.AddAsync(
                new RoleFeature
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    RoleId = adminRole.Id,
                    FeatureId = feature.Id,
                    CreatedAtUtc = utcNow,
                },
                cancellationToken);
            await db.RoleFeatures.AddAsync(
                new RoleFeature
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    RoleId = managerRole.Id,
                    FeatureId = feature.Id,
                    CreatedAtUtc = utcNow,
                },
                cancellationToken);
        }

        await db.TenantSettings.AddAsync(
            new TenantSettings
            {
                TenantId = tenant.Id,
                TradeName = "Jardín del Río",
                LegalName = "Copropiedad Condominio Jardín del Río",
                TaxRegime = "Régimen común",
                TaxId = "900765432-1",
                LegalRepresentative = "Demo Administrator",
                AddressLine = "Calle 19C # 44-45",
                City = "Pasto",
                Country = "Colombia",
                PostalCode = "520001",
                Phone = "+57 300 000 0001",
            },
            cancellationToken);

        await db.Employees.AddRangeAsync(
            [
                new Employee
                {
                    Id = DevelopmentSeedIds.AdminEmployeeId,
                    TenantId = tenant.Id,
                    TenantUserId = adminTu.Id,
                    FullName = "Demo Administrator",
                    JobTitle = "Administrator",
                    HiredOn = todaySafe(utcNow).AddYears(-2),
                    IsActive = true,
                    CreatedAtUtc = utcNow,
                },
                new Employee
                {
                    Id = DevelopmentSeedIds.ReceptionEmployeeId,
                    TenantId = tenant.Id,
                    TenantUserId = null,
                    FullName = "Reception Desk",
                    JobTitle = "Reception",
                    HiredOn = todaySafe(utcNow).AddMonths(-8),
                    IsActive = true,
                    CreatedAtUtc = utcNow,
                },
            ],
            cancellationToken);

        static DateOnly todaySafe(DateTime utc) => DateOnly.FromDateTime(utc);
    }

    private static async Task<(
        List<Floor> Floors,
        List<Unit> Apartments,
        List<Unit> Parkings,
        List<Person> People)> SeedStructureAndPeopleAsync(
        ApplicationDbContext db,
        Guid tenantId,
        DateTime utcNow,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        await db.Neighborhoods.AddRangeAsync(
            [
                new Neighborhood
                {
                    Id = DevelopmentSeedIds.NeighborhoodPandiacoId,
                    TenantId = tenantId,
                    Name = "Pandiaco",
                    City = "Pasto",
                    Country = "Colombia",
                    IsActive = true,
                    CreatedAtUtc = utcNow,
                },
                new Neighborhood
                {
                    Id = DevelopmentSeedIds.NeighborhoodMaridiazId,
                    TenantId = tenantId,
                    Name = "Maridiaz",
                    City = "Pasto",
                    Country = "Colombia",
                    IsActive = true,
                    CreatedAtUtc = utcNow,
                },
            ],
            cancellationToken);

        await db.HorizontalProperties.AddAsync(
            new HorizontalProperty
            {
                Id = DevelopmentSeedIds.PropertyId,
                TenantId = tenantId,
                Name = "Condominio Jardín del Río",
                TradeName = "Jardín del Río",
                LegalName = "Copropiedad Condominio Jardín del Río",
                Nit = "900765432-1",
                TaxRegime = "Régimen común",
                LegalRepresentative = "Demo Administrator",
                AddressLine = "Calle 19C # 44-45",
                City = "Pasto",
                Country = "Colombia",
                PostalCode = "520001",
                Phone = "+57 300 000 0001",
                NeighborhoodId = DevelopmentSeedIds.NeighborhoodPandiacoId,
                PropertyUseType = PropertyUseType.Residential,
                MatriculaInmobiliaria = "520-123456",
                RegulationNotes = "Seed reglamento summary for demo.",
                CoefficientBasisNotes = "Coeficientes proporcionales al área construida privada (m²), total 100.",
                IsActive = true,
                CreatedAtUtc = utcNow,
            },
            cancellationToken);

        await db.Buildings.AddAsync(
            new Building
            {
                Id = DevelopmentSeedIds.BuildingId,
                TenantId = tenantId,
                HorizontalPropertyId = DevelopmentSeedIds.PropertyId,
                Name = "Torre 1",
                Code = "T1",
                SortOrder = 1,
                IsActive = true,
                CreatedAtUtc = utcNow,
            },
            cancellationToken);

        var floors = new List<Floor>();
        for (var floorNumber = 2; floorNumber <= 4; floorNumber++)
        {
            var floor = new Floor
            {
                Id = Guid.Parse($"a100003{floorNumber}-003{floorNumber}-403{floorNumber}-803{floorNumber}-00000000003{floorNumber}"),
                TenantId = tenantId,
                BuildingId = DevelopmentSeedIds.BuildingId,
                Name = $"Floor {floorNumber}",
                Number = floorNumber,
                SortOrder = floorNumber,
                IsActive = true,
                CreatedAtUtc = utcNow,
            };
            floors.Add(floor);
        }

        await db.Floors.AddRangeAsync(floors, cancellationToken);

        var apartments = new List<Unit>();
        var aptIndex = 0;
        foreach (var floor in floors)
        {
            for (var unitOnFloor = 1; unitOnFloor <= 4; unitOnFloor++)
            {
                aptIndex++;
                var code = $"{floor.Number}{unitOnFloor:00}";
                apartments.Add(
                    new Unit
                    {
                        Id = Guid.Parse($"b2000000-0000-4000-8000-{aptIndex:D12}"),
                        TenantId = tenantId,
                        HorizontalPropertyId = DevelopmentSeedIds.PropertyId,
                        BuildingId = DevelopmentSeedIds.BuildingId,
                        FloorId = floor.Id,
                        Code = code,
                        UnitType = UnitType.Apartment,
                        PrivateBuiltAreaM2 = 68m + unitOnFloor * 2m,
                        PrivateOpenAreaM2 = 4m,
                        Coefficient = 0m,
                        IsBillable = true,
                        IsActive = true,
                        CreatedAtUtc = utcNow,
                    });
            }
        }

        var parkings = new List<Unit>();
        for (var i = 1; i <= ParkingCount; i++)
        {
            parkings.Add(
                new Unit
                {
                    Id = Guid.Parse($"b3000000-0000-4000-8000-{i:D12}"),
                    TenantId = tenantId,
                    HorizontalPropertyId = DevelopmentSeedIds.PropertyId,
                    BuildingId = DevelopmentSeedIds.BuildingId,
                    FloorId = null,
                    Code = $"P-{i:00}",
                    UnitType = UnitType.Parking,
                    PrivateBuiltAreaM2 = 12.5m,
                    Coefficient = 0m,
                    LinkedUnitId = apartments[i - 1].Id,
                    IsBillable = true,
                    IsActive = true,
                    CreatedAtUtc = utcNow,
                });
        }

        var allUnits = apartments.Concat(parkings).OrderBy(u => u.Code).ToList();
        CoefficientCalculator.ApplyAreaProportional(allUnits);

        await db.Units.AddRangeAsync(apartments, cancellationToken);
        await db.Units.AddRangeAsync(parkings, cancellationToken);

        var firstNames = new[]
        {
            "Ana", "Bruno", "Camila", "Diego", "Elena", "Felipe", "Gina", "Hugo", "Irene", "Jorge", "Karla", "Luis",
        };
        var lastNames = new[]
        {
            "Pérez", "Gómez", "Rodríguez", "López", "Martínez", "Sánchez", "Ramírez", "Torres", "Vargas", "Castro",
            "Moreno", "Ruiz",
        };

        var people = new List<Person>();
        for (var i = 0; i < AptCount; i++)
        {
            people.Add(
                new Person
                {
                    Id = Guid.Parse($"c1000000-0000-4000-8000-{(i + 1):D12}"),
                    TenantId = tenantId,
                    DocumentType = DocumentType.Cc,
                    DocumentNumber = $"{1000000000 + i + 1}",
                    FirstName = firstNames[i],
                    LastName = lastNames[i],
                    Email = $"{firstNames[i].ToLowerInvariant()}.owner@example.com",
                    Phone = $"+57 310 000 {i + 1:D4}",
                    IsActive = true,
                    CreatedAtUtc = utcNow,
                });
        }

        // Extra tenants/residents for a few units
        for (var i = 0; i < 4; i++)
        {
            people.Add(
                new Person
                {
                    Id = Guid.Parse($"c2000000-0000-4000-8000-{(i + 1):D12}"),
                    TenantId = tenantId,
                    DocumentType = DocumentType.Cc,
                    DocumentNumber = $"{2000000000 + i + 1}",
                    FirstName = $"Tenant{i + 1}",
                    LastName = lastNames[i],
                    Email = $"tenant{i + 1}@example.com",
                    Phone = $"+57 320 000 {i + 1:D4}",
                    IsActive = true,
                    CreatedAtUtc = utcNow,
                });
        }

        await db.People.AddRangeAsync(people, cancellationToken);

        var occupancies = new List<UnitOccupancy>();
        for (var i = 0; i < AptCount; i++)
        {
            occupancies.Add(
                new UnitOccupancy
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    UnitId = apartments[i].Id,
                    PersonId = people[i].Id,
                    Role = OccupancyRole.Owner,
                    IsPrimary = true,
                    ReceivesBilling = true,
                    StartDate = today.AddMonths(-18),
                    EndDate = null,
                    IsActive = true,
                    CreatedAtUtc = utcNow,
                });
        }

        for (var i = 0; i < 4; i++)
        {
            occupancies.Add(
                new UnitOccupancy
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    UnitId = apartments[i].Id,
                    PersonId = people[AptCount + i].Id,
                    Role = OccupancyRole.Tenant,
                    IsPrimary = false,
                    ReceivesBilling = false,
                    StartDate = today.AddMonths(-6),
                    EndDate = null,
                    IsActive = true,
                    CreatedAtUtc = utcNow,
                });
        }

        await db.UnitOccupancies.AddRangeAsync(occupancies, cancellationToken);

        await db.FeeSettings.AddAsync(
            new FeeSettings
            {
                HorizontalPropertyId = DevelopmentSeedIds.PropertyId,
                TenantId = tenantId,
                MonthlyBudgetAmount = MonthlyBudget,
                DueDayOfMonth = 5,
                LateInterestMonthlyPercent = 1.5m,
                Notes = "Demo ordinary budget distributed by coefficient.",
            },
            cancellationToken);

        return (floors, apartments, parkings, people);
    }

    private static async Task SeedBillingHistoryAsync(
        ApplicationDbContext db,
        Guid tenantId,
        List<Unit> billableUnits,
        List<Person> people,
        DateOnly today,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        // Oldest → newest: 5 previous months + current month
        var months = Enumerable.Range(0, 6)
            .Select(offset =>
            {
                var d = today.AddMonths(-(5 - offset));
                return (Year: d.Year, Month: d.Month, Offset: offset);
            })
            .ToList();

        var periods = new List<BillingPeriod>();
        foreach (var m in months)
        {
            var isCurrent = m.Offset == 5;
            periods.Add(
                new BillingPeriod
                {
                    Id = Guid.Parse($"d1000000-0000-4000-8000-{m.Year:D4}{m.Month:D2}000001"),
                    TenantId = tenantId,
                    HorizontalPropertyId = DevelopmentSeedIds.PropertyId,
                    Year = m.Year,
                    Month = m.Month,
                    Status = isCurrent ? BillingPeriodStatus.Open : BillingPeriodStatus.Closed,
                    Notes = isCurrent ? "Current open period (seed)" : "Closed historical period (seed)",
                    CreatedAtUtc = utcNow,
                });
        }

        await db.BillingPeriods.AddRangeAsync(periods, cancellationToken);

        var charges = new List<FeeCharge>();
        var payments = new List<Payment>();
        var allocations = new List<PaymentAllocation>();
        var rng = new Random(675201);

        for (var pIndex = 0; pIndex < periods.Count; pIndex++)
        {
            var period = periods[pIndex];
            var dueDay = Math.Min(5, DateTime.DaysInMonth(period.Year, period.Month));
            var dueDate = new DateOnly(period.Year, period.Month, dueDay);

            foreach (var unit in billableUnits)
            {
                var amount = Math.Round(
                    MonthlyBudget * (unit.Coefficient / CoefficientRules.Total),
                    2,
                    MidpointRounding.AwayFromZero);

                var charge = new FeeCharge
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    HorizontalPropertyId = DevelopmentSeedIds.PropertyId,
                    BillingPeriodId = period.Id,
                    UnitId = unit.Id,
                    ChargeType = FeeChargeType.Ordinary,
                    Description = $"Ordinary administration fee {period.Year}-{period.Month:D2}",
                    CoefficientSnapshot = unit.Coefficient,
                    Amount = amount,
                    AmountPaid = 0m,
                    DueDate = dueDate,
                    Status = FeeChargeStatus.Pending,
                    IsActive = true,
                    CreatedAtUtc = utcNow,
                };
                charges.Add(charge);

                // Payment profile by age of period
                var roll = rng.NextDouble();
                var payFully = pIndex switch
                {
                    <= 2 => roll < 0.88,
                    3 => roll < 0.75,
                    4 => roll < 0.55,
                    _ => roll < 0.35,
                };
                var payPartial = !payFully && pIndex >= 3 && roll < (pIndex == 5 ? 0.65 : 0.85);

                if (!payFully && !payPartial)
                    continue;

                var paidAmount = payFully
                    ? amount
                    : Math.Round(amount * (decimal)(0.35 + rng.NextDouble() * 0.4), 2, MidpointRounding.AwayFromZero);

                var paidDay = Math.Min(dueDay + rng.Next(0, 12), DateTime.DaysInMonth(period.Year, period.Month));
                var paidAt = new DateTime(period.Year, period.Month, paidDay, 15, 0, 0, DateTimeKind.Utc);
                if (paidAt > utcNow)
                    paidAt = utcNow.AddDays(-1);

                // Map parking to linked apartment owner when possible
                Person? payer = null;
                if (unit.UnitType == UnitType.Apartment)
                {
                    var aptIdx = apartmentsIndex(unit, billableUnits);
                    if (aptIdx >= 0 && aptIdx < AptCount)
                        payer = people[aptIdx];
                }
                else if (unit.LinkedUnitId is { } linked)
                {
                    var linkedApt = billableUnits.FirstOrDefault(u => u.Id == linked);
                    var aptIdx = linkedApt is null ? -1 : apartmentsIndex(linkedApt, billableUnits);
                    if (aptIdx >= 0 && aptIdx < AptCount)
                        payer = people[aptIdx];
                }

                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    HorizontalPropertyId = DevelopmentSeedIds.PropertyId,
                    UnitId = unit.Id,
                    PersonId = payer?.Id,
                    Amount = paidAmount,
                    PaidAt = paidAt,
                    Method = rng.Next(0, 3) switch
                    {
                        0 => PaymentMethod.Transfer,
                        1 => PaymentMethod.Cash,
                        _ => PaymentMethod.Check,
                    },
                    Reference = $"PAY-{period.Year}{period.Month:D2}-{unit.Code}",
                    Notes = payFully ? "Seed full payment" : "Seed partial payment",
                    IsActive = true,
                    CreatedAtUtc = utcNow,
                };
                payments.Add(payment);

                allocations.Add(
                    new PaymentAllocation
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        PaymentId = payment.Id,
                        FeeChargeId = charge.Id,
                        Amount = paidAmount,
                        CreatedAtUtc = utcNow,
                    });

                charge.AmountPaid = paidAmount;
                charge.Status = paidAmount >= charge.Amount
                    ? FeeChargeStatus.Paid
                    : FeeChargeStatus.Partial;
            }
        }

        // Interest on a few chronically overdue ordinary charges (oldest unpaid)
        var currentPeriod = periods[^1];
        var overdueUnpaid = charges
            .Where(c =>
                c.ChargeType == FeeChargeType.Ordinary
                && c.Status is FeeChargeStatus.Pending or FeeChargeStatus.Partial
                && c.DueDate < today.AddMonths(-2))
            .Take(4)
            .ToList();

        foreach (var overdue in overdueUnpaid)
        {
            var balance = overdue.Amount - overdue.AmountPaid;
            var interest = Math.Round(balance * 0.015m, 2, MidpointRounding.AwayFromZero);
            if (interest <= 0)
                continue;

            charges.Add(
                new FeeCharge
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    HorizontalPropertyId = DevelopmentSeedIds.PropertyId,
                    BillingPeriodId = currentPeriod.Id,
                    UnitId = overdue.UnitId,
                    ChargeType = FeeChargeType.Interest,
                    Description = $"Late interest on {overdue.Description}",
                    CoefficientSnapshot = overdue.CoefficientSnapshot,
                    Amount = interest,
                    AmountPaid = 0m,
                    DueDate = today,
                    Status = FeeChargeStatus.Pending,
                    IsActive = true,
                    CreatedAtUtc = utcNow,
                });
        }

        await db.FeeCharges.AddRangeAsync(charges, cancellationToken);
        await db.Payments.AddRangeAsync(payments, cancellationToken);
        await db.PaymentAllocations.AddRangeAsync(allocations, cancellationToken);

        static int apartmentsIndex(Unit unit, List<Unit> all)
        {
            var apts = all.Where(u => u.UnitType == UnitType.Apartment).OrderBy(u => u.Code).ToList();
            return apts.FindIndex(u => u.Id == unit.Id);
        }
    }

    private static async Task SeedAmenitiesAndReceptionAsync(
        ApplicationDbContext db,
        Guid tenantId,
        List<Unit> apartments,
        List<Person> people,
        DateOnly today,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var areas = new[]
        {
            new CommonArea
            {
                Id = DevelopmentSeedIds.SocialRoomId,
                TenantId = tenantId,
                HorizontalPropertyId = DevelopmentSeedIds.PropertyId,
                Name = "Social room",
                AreaType = CommonAreaType.SocialRoom,
                Capacity = 40,
                RequiresDeposit = true,
                DepositAmount = 200_000m,
                RulesNotes = "No loud music after 10pm.",
                MinDurationMinutes = 60,
                MaxDurationMinutes = 360,
                IsActive = true,
                CreatedAtUtc = utcNow,
            },
            new CommonArea
            {
                Id = DevelopmentSeedIds.BbqId,
                TenantId = tenantId,
                HorizontalPropertyId = DevelopmentSeedIds.PropertyId,
                Name = "BBQ area",
                AreaType = CommonAreaType.Bbq,
                Capacity = 20,
                RequiresDeposit = true,
                DepositAmount = 150_000m,
                IsActive = true,
                CreatedAtUtc = utcNow,
            },
            new CommonArea
            {
                Id = DevelopmentSeedIds.CourtId,
                TenantId = tenantId,
                HorizontalPropertyId = DevelopmentSeedIds.PropertyId,
                Name = "Multipurpose court",
                AreaType = CommonAreaType.Court,
                Capacity = 12,
                RequiresDeposit = false,
                MinDurationMinutes = 60,
                MaxDurationMinutes = 120,
                IsActive = true,
                CreatedAtUtc = utcNow,
            },
        };
        await db.CommonAreas.AddRangeAsync(areas, cancellationToken);

        var reservations = new List<Reservation>();
        var reception = new List<ReceptionItem>();
        var rng = new Random(675202);
        var areaIds = areas.Select(a => a.Id).ToArray();

        for (var monthOffset = 5; monthOffset >= 0; monthOffset--)
        {
            var monthDate = today.AddMonths(-monthOffset);
            var reservationsThisMonth = monthOffset == 0 ? 3 : 4;

            for (var r = 0; r < reservationsThisMonth; r++)
            {
                var day = Math.Min(5 + r * 5, DateTime.DaysInMonth(monthDate.Year, monthDate.Month));
                var startLocal = new DateTime(monthDate.Year, monthDate.Month, day, 14 + r, 0, 0, DateTimeKind.Utc);
                var unit = apartments[rng.Next(apartments.Count)];
                var person = people[Math.Min(apartments.IndexOf(unit), people.Count - 1)];
                var status = monthOffset switch
                {
                    0 when r == 0 => ReservationStatus.Pending,
                    0 => ReservationStatus.Confirmed,
                    1 when r == 3 => ReservationStatus.Cancelled,
                    _ => ReservationStatus.Completed,
                };

                reservations.Add(
                    new Reservation
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        HorizontalPropertyId = DevelopmentSeedIds.PropertyId,
                        CommonAreaId = areaIds[r % areaIds.Length],
                        UnitId = unit.Id,
                        PersonId = person.Id,
                        StartsAtUtc = startLocal,
                        EndsAtUtc = startLocal.AddHours(3),
                        Status = status,
                        AttendeesCount = 4 + rng.Next(0, 10),
                        Notes = "Seed reservation",
                        DepositPaid = status is ReservationStatus.Confirmed or ReservationStatus.Completed,
                        IsActive = true,
                        CreatedAtUtc = utcNow,
                    });
            }

            var packages = 4;
            for (var p = 0; p < packages; p++)
            {
                var day = Math.Min(3 + p * 6, DateTime.DaysInMonth(monthDate.Year, monthDate.Month));
                var received = new DateTime(monthDate.Year, monthDate.Month, day, 10 + p, 30, 0, DateTimeKind.Utc);
                var unit = apartments[p % apartments.Count];
                var person = people[p % AptCount];
                var isPending = monthOffset == 0 && p >= 2;
                var status = isPending
                    ? ReceptionItemStatus.PendingPickup
                    : p == 0 && monthOffset > 2
                        ? ReceptionItemStatus.Returned
                        : ReceptionItemStatus.Delivered;

                reception.Add(
                    new ReceptionItem
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        HorizontalPropertyId = DevelopmentSeedIds.PropertyId,
                        UnitId = unit.Id,
                        PersonId = person.Id,
                        ItemType = (p % 3) switch
                        {
                            0 => ReceptionItemType.Package,
                            1 => ReceptionItemType.Letter,
                            _ => ReceptionItemType.Document,
                        },
                        Status = status,
                        ReceivedAtUtc = received,
                        ReceivedByEmployeeId = DevelopmentSeedIds.ReceptionEmployeeId,
                        DeliveredAtUtc = status == ReceptionItemStatus.Delivered
                            ? received.AddHours(6 + p)
                            : status == ReceptionItemStatus.Returned ? received.AddDays(2) : null,
                        DeliveredByEmployeeId = status is ReceptionItemStatus.Delivered or ReceptionItemStatus.Returned
                            ? DevelopmentSeedIds.ReceptionEmployeeId
                            : null,
                        CarrierOrSender = p % 2 == 0 ? "Servientrega" : "Interrapidisimo",
                        TrackingOrReference = $"TRK-{monthDate.Year}{monthDate.Month:D2}-{p + 1:D3}",
                        RecipientName = $"{person.FirstName} {person.LastName}",
                        Notes = "Seed reception item",
                        IsActive = true,
                        CreatedAtUtc = utcNow,
                    });
            }
        }

        await db.Reservations.AddRangeAsync(reservations, cancellationToken);
        await db.ReceptionItems.AddRangeAsync(reception, cancellationToken);
    }
}
