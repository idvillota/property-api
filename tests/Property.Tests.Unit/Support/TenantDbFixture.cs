using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Infrastructure.Common;
using Property.Infrastructure.Persistence;

namespace Property.Tests.Unit.Support;

/// <summary>
/// Shared EF Core in-memory database + tenant context for service-level tests.
/// </summary>
public sealed class TenantDbFixture : IDisposable
{
    public Guid TenantId { get; }
    public CurrentTenantContext TenantContext { get; }
    public ApplicationDbContext Db { get; }

    public TenantDbFixture()
    {
        TenantId = Guid.NewGuid();
        TenantContext = new CurrentTenantContext { TenantId = TenantId };
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        Db = new ApplicationDbContext(options, TenantContext);
    }

    public IRepository<T> Repository<T>()
        where T : class =>
        new Repository<T>(Db);

    public IUnitOfWork UnitOfWork => new UnitOfWork(Db);

    public void Dispose() => Db.Dispose();
}

/// <summary>
/// In-memory database without tenant resolution (registration / login flows).
/// </summary>
public sealed class NoTenantDbFixture : IDisposable
{
    public ApplicationDbContext Db { get; }
    public CurrentTenantContext TenantContext { get; }

    public NoTenantDbFixture()
    {
        TenantContext = new CurrentTenantContext();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        Db = new ApplicationDbContext(options, TenantContext);
    }

    public IUnitOfWork UnitOfWork => new UnitOfWork(Db);

    public void Dispose() => Db.Dispose();
}
