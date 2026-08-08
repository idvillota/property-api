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
    public DbSet<Person> People => Set<Person>();
    public DbSet<UnitOccupancy> UnitOccupancies => Set<UnitOccupancy>();

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
            e.Property(x => x.Nit).HasMaxLength(40);
            e.Property(x => x.AddressLine).HasMaxLength(300);
            e.Property(x => x.City).HasMaxLength(120);
            e.Property(x => x.Country).HasMaxLength(80);
            e.Property(x => x.MatriculaInmobiliaria).HasMaxLength(80);
            e.Property(x => x.RegulationNotes).HasMaxLength(4000);
            e.Property(x => x.CoefficientBasisNotes).HasMaxLength(4000);
            e.HasOne(x => x.Neighborhood)
                .WithMany()
                .HasForeignKey(x => x.NeighborhoodId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.Name });
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

        modelBuilder.Entity<Person>(e =>
        {
            e.Property(x => x.DocumentNumber).HasMaxLength(40);
            e.Property(x => x.FirstName).HasMaxLength(120);
            e.Property(x => x.LastName).HasMaxLength(120);
            e.Property(x => x.Email).HasMaxLength(320);
            e.Property(x => x.Phone).HasMaxLength(40);
            e.HasIndex(x => new { x.TenantId, x.DocumentType, x.DocumentNumber }).IsUnique();
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

        modelBuilder.Entity<Person>().HasQueryFilter(e =>
            _currentTenant.TenantId == null || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<UnitOccupancy>().HasQueryFilter(e =>
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
