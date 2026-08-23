using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Domain.Common;
using Property.Domain.Entities;

namespace Property.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext
{
    private readonly ICurrentTenantContext _currentTenant;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentTenantContext currentTenant)
        : base(options)
    {
        _currentTenant = currentTenant;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<RoleFeature> RoleFeatures => Set<RoleFeature>();
    public DbSet<TenantUserRole> TenantUserRoles => Set<TenantUserRole>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<TenantSettings> TenantSettings => Set<TenantSettings>();
    public DbSet<Neighborhood> Neighborhoods => Set<Neighborhood>();
    public DbSet<HorizontalProperty> HorizontalProperties => Set<HorizontalProperty>();
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<VehicleBrand> VehicleBrands => Set<VehicleBrand>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Person> People => Set<Person>();
    public DbSet<UserPersonLink> UserPersonLinks => Set<UserPersonLink>();
    public DbSet<UnitOccupancy> UnitOccupancies => Set<UnitOccupancy>();
    public DbSet<FeeSettings> FeeSettings => Set<FeeSettings>();
    public DbSet<BillingPeriod> BillingPeriods => Set<BillingPeriod>();
    public DbSet<FeeCharge> FeeCharges => Set<FeeCharge>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<PaymentIntent> PaymentIntents => Set<PaymentIntent>();
    public DbSet<CommonArea> CommonAreas => Set<CommonArea>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReceptionItem> ReceptionItems => Set<ReceptionItem>();
    public DbSet<MuralPost> MuralPosts => Set<MuralPost>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<FileAsset> FileAssets => Set<FileAsset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(x => x.NormalizedEmail).IsUnique();
            e.Property(x => x.Email).HasMaxLength(320);
            e.Property(x => x.NormalizedEmail).HasMaxLength(320);
        });

        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Slug).HasMaxLength(120);
            e.Property(x => x.CurrencyCode).HasMaxLength(8);
        });

        modelBuilder.Entity<TenantUser>(e =>
        {
            e.HasOne(x => x.Tenant).WithMany(x => x.TenantUsers).HasForeignKey(x => x.TenantId);
            e.HasOne(x => x.User).WithMany(x => x.TenantUsers).HasForeignKey(x => x.UserId);
            e.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();
            e.Property(x => x.BrandTheme).HasMaxLength(32);
            e.Property(x => x.ColorScheme).HasMaxLength(16);
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId);
            e.HasIndex(x => new { x.TenantId, x.NormalizedName }).IsUnique();
            e.Property(x => x.Name).HasMaxLength(120);
            e.Property(x => x.NormalizedName).HasMaxLength(120);
        });

        modelBuilder.Entity<TenantUserRole>(e =>
        {
            e.HasOne(x => x.TenantUser).WithMany(x => x.TenantUserRoles).HasForeignKey(x => x.TenantUserId);
            e.HasOne(x => x.Role).WithMany(x => x.TenantUserRoles).HasForeignKey(x => x.RoleId);
            e.HasIndex(x => new { x.TenantUserId, x.RoleId }).IsUnique();
        });

        modelBuilder.Entity<Feature>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(80);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Module).HasMaxLength(80);
        });

        modelBuilder.Entity<RoleFeature>(e =>
        {
            e.HasOne(x => x.Role).WithMany(x => x.RoleFeatures).HasForeignKey(x => x.RoleId);
            e.HasOne(x => x.Feature).WithMany(x => x.RoleFeatures).HasForeignKey(x => x.FeatureId);
            e.HasIndex(x => new { x.RoleId, x.FeatureId }).IsUnique();
        });

        modelBuilder.Entity<Employee>(e =>
        {
            e.HasOne(x => x.TenantUser).WithOne(x => x.Employee).HasForeignKey<Employee>(x => x.TenantUserId);
        });

        modelBuilder.Entity<TenantSettings>(e =>
        {
            e.HasKey(x => x.TenantId);
            e.HasOne(x => x.Tenant).WithOne().HasForeignKey<TenantSettings>(x => x.TenantId);
            e.Property(x => x.TradeName).HasMaxLength(200);
            e.Property(x => x.LegalName).HasMaxLength(200);
            e.Property(x => x.TaxRegime).HasMaxLength(80);
            e.Property(x => x.TaxId).HasMaxLength(40);
            e.Property(x => x.LegalRepresentative).HasMaxLength(200);
            e.Property(x => x.AddressLine).HasMaxLength(300);
            e.Property(x => x.City).HasMaxLength(120);
            e.Property(x => x.Country).HasMaxLength(80);
            e.Property(x => x.PostalCode).HasMaxLength(20);
            e.Property(x => x.Phone).HasMaxLength(40);
            e.Property(x => x.DashboardLayoutJson);
        });

        modelBuilder.Entity<Neighborhood>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.City).HasMaxLength(120);
            e.Property(x => x.Country).HasMaxLength(80);
            e.HasIndex(x => new { x.TenantId, x.Name });
        });

        modelBuilder.Entity<HorizontalProperty>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.TradeName).HasMaxLength(200);
            e.Property(x => x.LegalName).HasMaxLength(200);
            e.Property(x => x.Nit).HasMaxLength(40);
            e.Property(x => x.TaxRegime).HasMaxLength(80);
            e.Property(x => x.LegalRepresentative).HasMaxLength(200);
            e.Property(x => x.AddressLine).HasMaxLength(300);
            e.Property(x => x.City).HasMaxLength(120);
            e.Property(x => x.Country).HasMaxLength(80);
            e.Property(x => x.PostalCode).HasMaxLength(20);
            e.Property(x => x.Phone).HasMaxLength(40);
            e.Property(x => x.MatriculaInmobiliaria).HasMaxLength(80);
            e.Property(x => x.RegulationNotes).HasMaxLength(4000);
            e.Property(x => x.CoefficientBasisNotes).HasMaxLength(4000);
            e.HasOne(x => x.Neighborhood)
                .WithMany()
                .HasForeignKey(x => x.NeighborhoodId)
                .OnDelete(DeleteBehavior.Restrict);
            // Colombia model: one SaaS tenant administers exactly one horizontal property.
            e.HasIndex(x => x.TenantId).IsUnique();
        });

        modelBuilder.Entity<Building>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Code).HasMaxLength(40);
            e.HasOne(x => x.HorizontalProperty)
                .WithMany(x => x.Buildings)
                .HasForeignKey(x => x.HorizontalPropertyId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.HorizontalPropertyId, x.Name }).IsUnique();
        });

        modelBuilder.Entity<Floor>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.HasOne(x => x.Building)
                .WithMany(x => x.Floors)
                .HasForeignKey(x => x.BuildingId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.BuildingId, x.Name }).IsUnique();
        });

        modelBuilder.Entity<Unit>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(80);
            e.Property(x => x.ContributionModuleCode).HasMaxLength(40);
            e.Property(x => x.PrivateBuiltAreaM2).HasPrecision(18, 2);
            e.Property(x => x.PrivateOpenAreaM2).HasPrecision(18, 2);
            e.Property(x => x.Coefficient).HasPrecision(18, 5);
            e.HasOne(x => x.HorizontalProperty)
                .WithMany(x => x.Units)
                .HasForeignKey(x => x.HorizontalPropertyId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Building)
                .WithMany()
                .HasForeignKey(x => x.BuildingId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Floor)
                .WithMany()
                .HasForeignKey(x => x.FloorId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.LinkedUnit)
                .WithMany()
                .HasForeignKey(x => x.LinkedUnitId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.HorizontalPropertyId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<VehicleBrand>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(80);
            e.HasIndex(x => new { x.TenantId, x.Name });
        });

        modelBuilder.Entity<Vehicle>(e =>
        {
            e.Property(x => x.Brand).HasMaxLength(80);
            e.Property(x => x.Model).HasMaxLength(80);
            e.Property(x => x.Color).HasMaxLength(40);
            e.Property(x => x.Plate).HasMaxLength(20);
            e.HasOne(x => x.Unit)
                .WithMany()
                .HasForeignKey(x => x.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.VehicleBrand)
                .WithMany()
                .HasForeignKey(x => x.VehicleBrandId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.Plate });
            e.HasIndex(x => x.UnitId);
            e.HasIndex(x => x.VehicleBrandId);
            e.HasIndex(x => new { x.TenantId, x.HorizontalPropertyId });
        });

        modelBuilder.Entity<Person>(e =>
        {
            e.Property(x => x.DocumentNumber).HasMaxLength(40);
            e.Property(x => x.FirstName).HasMaxLength(120);
            e.Property(x => x.LastName).HasMaxLength(120);
            e.Property(x => x.Email).HasMaxLength(320);
            e.Property(x => x.Phone).HasMaxLength(40);
            e.HasIndex(x => new { x.TenantId, x.DocumentType, x.DocumentNumber }).IsUnique();
        });

        modelBuilder.Entity<UserPersonLink>(e =>
        {
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Person)
                .WithMany()
                .HasForeignKey(x => x.PersonId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.UserId })
                .IsUnique()
                .HasFilter("\"IsActive\" = TRUE");
            e.HasIndex(x => new { x.TenantId, x.PersonId })
                .IsUnique()
                .HasFilter("\"IsActive\" = TRUE");
        });

        modelBuilder.Entity<UnitOccupancy>(e =>
        {
            e.HasOne(x => x.Unit)
                .WithMany()
                .HasForeignKey(x => x.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Person)
                .WithMany()
                .HasForeignKey(x => x.PersonId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.UnitId, x.PersonId, x.StartDate });
        });

        modelBuilder.Entity<FeeSettings>(e =>
        {
            e.HasKey(x => x.HorizontalPropertyId);
            e.Property(x => x.MonthlyBudgetAmount).HasPrecision(18, 2);
            e.Property(x => x.LateInterestMonthlyPercent).HasPrecision(18, 2);
            e.Property(x => x.Notes).HasMaxLength(4000);
            e.HasOne(x => x.HorizontalProperty)
                .WithOne()
                .HasForeignKey<FeeSettings>(x => x.HorizontalPropertyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BillingPeriod>(e =>
        {
            e.Property(x => x.Notes).HasMaxLength(4000);
            e.HasOne(x => x.HorizontalProperty)
                .WithMany()
                .HasForeignKey(x => x.HorizontalPropertyId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.HorizontalPropertyId, x.Year, x.Month }).IsUnique();
        });

        modelBuilder.Entity<FeeCharge>(e =>
        {
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.CoefficientSnapshot).HasPrecision(18, 5);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.AmountPaid).HasPrecision(18, 2);
            e.HasOne(x => x.HorizontalProperty)
                .WithMany()
                .HasForeignKey(x => x.HorizontalPropertyId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.BillingPeriod)
                .WithMany(x => x.FeeCharges)
                .HasForeignKey(x => x.BillingPeriodId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Unit)
                .WithMany()
                .HasForeignKey(x => x.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.HorizontalPropertyId, x.BillingPeriodId, x.UnitId, x.ChargeType });
            e.HasIndex(x => new { x.TenantId, x.UnitId, x.Status });
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Reference).HasMaxLength(120);
            e.Property(x => x.Notes).HasMaxLength(4000);
            e.HasOne(x => x.HorizontalProperty)
                .WithMany()
                .HasForeignKey(x => x.HorizontalPropertyId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Unit)
                .WithMany()
                .HasForeignKey(x => x.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Person)
                .WithMany()
                .HasForeignKey(x => x.PersonId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.HorizontalPropertyId, x.UnitId });
        });

        modelBuilder.Entity<PaymentAllocation>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasOne(x => x.Payment)
                .WithMany(x => x.Allocations)
                .HasForeignKey(x => x.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.FeeCharge)
                .WithMany(x => x.Allocations)
                .HasForeignKey(x => x.FeeChargeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.PaymentId, x.FeeChargeId }).IsUnique();
        });

        modelBuilder.Entity<PaymentIntent>(e =>
        {
            e.Property(x => x.IdempotencyKey).HasMaxLength(120);
            e.Property(x => x.ProviderRef).HasMaxLength(120);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.AllocationsJson).HasColumnType("text");
            e.Property(x => x.FailureReason).HasMaxLength(500);
            e.HasOne(x => x.HorizontalProperty)
                .WithMany()
                .HasForeignKey(x => x.HorizontalPropertyId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Unit)
                .WithMany()
                .HasForeignKey(x => x.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Person)
                .WithMany()
                .HasForeignKey(x => x.PersonId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Payment)
                .WithMany()
                .HasForeignKey(x => x.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.Status, x.CreatedAtUtc });
        });

        modelBuilder.Entity<CommonArea>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.DepositAmount).HasPrecision(18, 2);
            e.Property(x => x.RulesNotes).HasMaxLength(4000);
            e.HasOne(x => x.HorizontalProperty)
                .WithMany(x => x.CommonAreas)
                .HasForeignKey(x => x.HorizontalPropertyId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.HorizontalPropertyId, x.Name }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.HorizontalPropertyId, x.AreaType });
        });

        modelBuilder.Entity<Reservation>(e =>
        {
            e.Property(x => x.Notes).HasMaxLength(4000);
            e.HasOne(x => x.HorizontalProperty)
                .WithMany()
                .HasForeignKey(x => x.HorizontalPropertyId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CommonArea)
                .WithMany(x => x.Reservations)
                .HasForeignKey(x => x.CommonAreaId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Unit)
                .WithMany()
                .HasForeignKey(x => x.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Person)
                .WithMany()
                .HasForeignKey(x => x.PersonId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.CommonAreaId, x.StartsAtUtc });
            e.HasIndex(x => new { x.TenantId, x.HorizontalPropertyId, x.Status });
        });

        modelBuilder.Entity<ReceptionItem>(e =>
        {
            e.Property(x => x.CarrierOrSender).HasMaxLength(200);
            e.Property(x => x.TrackingOrReference).HasMaxLength(120);
            e.Property(x => x.RecipientName).HasMaxLength(200);
            e.Property(x => x.AuthorizedPickupName1).HasMaxLength(200);
            e.Property(x => x.AuthorizedPickupName2).HasMaxLength(200);
            e.Property(x => x.DeliveredToExternalName).HasMaxLength(200);
            e.Property(x => x.Notes).HasMaxLength(4000);
            e.HasOne(x => x.HorizontalProperty)
                .WithMany()
                .HasForeignKey(x => x.HorizontalPropertyId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Unit)
                .WithMany()
                .HasForeignKey(x => x.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Person)
                .WithMany()
                .HasForeignKey(x => x.PersonId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.DeliveredToPerson)
                .WithMany()
                .HasForeignKey(x => x.DeliveredToPersonId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ReceivedByEmployee)
                .WithMany()
                .HasForeignKey(x => x.ReceivedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.DeliveredByEmployee)
                .WithMany()
                .HasForeignKey(x => x.DeliveredByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.HorizontalPropertyId, x.Status });
            e.HasIndex(x => new { x.TenantId, x.HorizontalPropertyId, x.Direction, x.Status });
            e.HasIndex(x => new { x.TenantId, x.UnitId, x.Status });
        });

        modelBuilder.Entity<MuralPost>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Body).HasMaxLength(4000);
            e.Property(x => x.ContactPhone).HasMaxLength(40);
            e.Property(x => x.ContactEmail).HasMaxLength(320);
            e.Property(x => x.ContactNote).HasMaxLength(200);
            e.Property(x => x.AreaLabel).HasMaxLength(120);
            e.Property(x => x.AuthorDisplayName).HasMaxLength(200);
            e.HasOne(x => x.HorizontalProperty)
                .WithMany()
                .HasForeignKey(x => x.HorizontalPropertyId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Unit)
                .WithMany()
                .HasForeignKey(x => x.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.HorizontalPropertyId, x.IsPinned, x.PublishedAtUtc });
            e.HasIndex(x => new { x.TenantId, x.HorizontalPropertyId, x.Kind, x.Category });
        });

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.Property(x => x.MessageType).HasMaxLength(120);
            e.Property(x => x.PayloadJson).HasColumnType("text");
            e.Property(x => x.LastError).HasMaxLength(2000);
            e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.Status, x.NextAttemptUtc });
            e.HasIndex(x => new { x.TenantId, x.Status, x.CreatedAtUtc });
        });

        modelBuilder.Entity<FileAsset>(e =>
        {
            e.Property(x => x.StorageKey).HasMaxLength(500);
            e.Property(x => x.FileName).HasMaxLength(255);
            e.Property(x => x.ContentType).HasMaxLength(120);
            e.Property(x => x.Purpose).HasMaxLength(80);
            e.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.TenantId, x.StorageKey }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.CreatedAtUtc });
        });

        ApplyTenantFilters(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    private void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantUser>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<Role>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<RoleFeature>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<TenantUserRole>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<Employee>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<TenantSettings>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<Neighborhood>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<HorizontalProperty>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<Building>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<Floor>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<Unit>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<VehicleBrand>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<Vehicle>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<Person>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<UserPersonLink>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<UnitOccupancy>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<FeeSettings>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<BillingPeriod>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<FeeCharge>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<Payment>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<PaymentAllocation>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<PaymentIntent>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<CommonArea>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<Reservation>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<ReceptionItem>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<MuralPost>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<OutboxMessage>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<FileAsset>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utc = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<EntityBase>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = utc;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = utc;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ITenantScoped>())
        {
            if (entry.State == EntityState.Added && _currentTenant.TenantId.HasValue)
            {
                entry.Entity.TenantId = _currentTenant.TenantId.Value;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
