using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Property.Application.Common.Interfaces;
using Property.Infrastructure.Common;

namespace Property.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var infrastructureProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var apiProjectRoot = Path.GetFullPath(Path.Combine(infrastructureProjectRoot, "..", "Property.Api"));

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiProjectRoot)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                $"Connection string 'DefaultConnection' not found. Expected appsettings under: {apiProjectRoot}");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        ICurrentTenantContext tenant = new CurrentTenantContext();
        return new ApplicationDbContext(options, tenant);
    }
}
