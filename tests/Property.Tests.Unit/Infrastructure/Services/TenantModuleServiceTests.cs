using System.Reflection;
using Property.Api.Authorization;
using Property.Api.Controllers;
using Property.Application.Authorization;
using Property.Application.Features.Organization.TenantSettings;
using Property.Domain.Entities;
using Property.Infrastructure.Services;
using Property.Infrastructure.Services.Accounting;
using Property.Tests.Unit.Support;

namespace Property.Tests.Unit.Infrastructure.Services;

public sealed class TenantModuleServiceTests
{
    [Fact]
    public async Task ModuleFlag_PortalOff_BlocksPortalApi()
    {
        using var fx = new TenantDbFixture();
        var sut = new TenantModuleService(fx.Db, fx.TenantContext, new ChartOfAccountsService(fx.Db, fx.TenantContext));

        await sut.UpdateFlagsAsync(new TenantModuleFlagsDto
        {
            Portal = false,
            DigitalPayments = false,
            Accounting = false,
            Visits = false,
            Assemblies = false,
        });

        Assert.False(await sut.IsEnabledAsync(TenantModuleKeys.Portal));

        var portalAttr = typeof(PortalController).GetCustomAttribute<RequireTenantModuleAttribute>();
        Assert.NotNull(portalAttr);
        Assert.Equal(TenantModuleKeys.Portal, portalAttr!.ModuleKey);
    }

    [Fact]
    public async Task ModuleFlag_Accounting_IndependentOfAssemblies()
    {
        using var fx = new TenantDbFixture();
        var sut = new TenantModuleService(fx.Db, fx.TenantContext, new ChartOfAccountsService(fx.Db, fx.TenantContext));

        await sut.UpdateFlagsAsync(new TenantModuleFlagsDto
        {
            Portal = true,
            Accounting = true,
            Assemblies = false,
        });

        Assert.True(await sut.IsEnabledAsync(TenantModuleKeys.Accounting));
        Assert.False(await sut.IsEnabledAsync(TenantModuleKeys.Assemblies));

        var settings = await fx.Db.TenantSettings.FindAsync(fx.TenantId);
        Assert.NotNull(settings);
        Assert.True(settings!.ModuleAccountingEnabled);
        Assert.False(settings.ModuleAssembliesEnabled);
    }

    [Fact]
    public async Task GetFlagsAsync_Defaults_PortalOn_OthersOff()
    {
        using var fx = new TenantDbFixture();
        var sut = new TenantModuleService(fx.Db, fx.TenantContext, new ChartOfAccountsService(fx.Db, fx.TenantContext));

        var flags = await sut.GetFlagsAsync();

        Assert.True(flags.Portal);
        Assert.False(flags.DigitalPayments);
        Assert.False(flags.Accounting);
        Assert.False(flags.Visits);
        Assert.False(flags.Assemblies);
    }
}
