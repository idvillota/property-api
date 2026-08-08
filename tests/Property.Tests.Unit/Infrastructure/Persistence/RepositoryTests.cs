using Microsoft.EntityFrameworkCore;
using Property.Domain.Entities;
using Property.Infrastructure.Persistence;
using Property.Tests.Unit.Support;

namespace Property.Tests.Unit.Infrastructure.Persistence;

public sealed class RepositoryTests
{
    [Fact]
    public async Task Add_and_unit_of_work_save_persist_entity()
    {
        using var fx = new TenantDbFixture();
        var repo = fx.Repository<Employee>();
        await repo.AddAsync(
            new Employee
            {
                Id = Guid.NewGuid(),
                FullName = "Test Employee",
                IsActive = true,
            },
            CancellationToken.None);
        await fx.UnitOfWork.SaveChangesAsync();

        Assert.Equal(1, await fx.Db.Employees.CountAsync());
    }

    [Fact]
    public async Task GetByIdAsync_returns_tracked_entity()
    {
        using var fx = new TenantDbFixture();
        var id = Guid.NewGuid();
        fx.Db.Employees.Add(
            new Employee
            {
                Id = id,
                TenantId = fx.TenantId,
                FullName = "Tracked",
                IsActive = true,
            });
        await fx.Db.SaveChangesAsync();
        fx.Db.ChangeTracker.Clear();

        var repo = new Repository<Employee>(fx.Db);
        var entity = await repo.GetByIdAsync(id);

        Assert.NotNull(entity);
        Assert.Equal("Tracked", entity!.FullName);
    }
}
