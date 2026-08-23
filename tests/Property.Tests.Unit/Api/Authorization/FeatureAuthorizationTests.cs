using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Property.Api.Authorization;
using Property.Api.Controllers;
using Property.Application.Authorization;
using Property.Infrastructure.Identity;

namespace Property.Tests.Unit.Api.Authorization;

public sealed class FeatureAuthorizationTests
{
    [Fact]
    public void FeatureAuthorizationPolicies_uses_stable_prefix()
    {
        var policy = FeatureAuthorizationPolicies.For(FeatureCodes.OrganizationTeam);
        Assert.Equal("Feature:organization.team", policy);
    }

    [Fact]
    public void PermissionClaims_matches_jwt_token_service()
    {
        Assert.Equal(JwtTokenService.PermissionClaimType, PermissionClaims.Type);
    }

    [Fact]
    public async Task AddFeatureAuthorization_registers_policy_per_feature_code()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeatureAuthorization();

        using var provider = services.BuildServiceProvider();
        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await policyProvider.GetPolicyAsync(
            FeatureAuthorizationPolicies.For(FeatureCodes.SettingsTenant));

        Assert.NotNull(policy);
        Assert.NotEmpty(policy!.Requirements);
    }

    [Theory]
    [InlineData(typeof(TenantUsersController), FeatureCodes.OrganizationTeam)]
    [InlineData(typeof(RolePermissionsController), FeatureCodes.OrganizationRoles)]
    [InlineData(typeof(EmployeesController), FeatureCodes.OrganizationEmployees)]
    [InlineData(typeof(TenantSettingsController), FeatureCodes.SettingsTenant)]
    [InlineData(typeof(NeighborhoodsController), FeatureCodes.StructureNeighborhoods)]
    [InlineData(typeof(HorizontalPropertiesController), FeatureCodes.StructureProperties)]
    [InlineData(typeof(BuildingsController), FeatureCodes.StructureBuildings)]
    [InlineData(typeof(StructureComposerController), FeatureCodes.StructureBuildings)]
    [InlineData(typeof(FloorsController), FeatureCodes.StructureFloors)]
    [InlineData(typeof(UnitsController), FeatureCodes.StructureUnits)]
    [InlineData(typeof(PeopleController), FeatureCodes.PeopleManage)]
    [InlineData(typeof(UnitOccupanciesController), FeatureCodes.PeopleManage)]
    [InlineData(typeof(FeeSettingsController), FeatureCodes.BillingSettings)]
    [InlineData(typeof(BillingPeriodsController), FeatureCodes.BillingCharges)]
    [InlineData(typeof(FeeChargesController), FeatureCodes.BillingCharges)]
    [InlineData(typeof(PaymentsController), FeatureCodes.BillingPayments)]
    [InlineData(typeof(PaymentIntentsController), FeatureCodes.BillingPayments)]
    [InlineData(typeof(CommonAreasController), FeatureCodes.AmenitiesAreas)]
    [InlineData(typeof(ReservationsController), FeatureCodes.AmenitiesReservations)]
    [InlineData(typeof(ReceptionItemsController), FeatureCodes.ReceptionManage)]
    [InlineData(typeof(ReportsController), FeatureCodes.ReportsView)]
    [InlineData(typeof(AuditEventsController), FeatureCodes.AuditView)]
    [InlineData(typeof(PortalController), FeatureCodes.PortalAccess)]
    public void Secured_controllers_require_matching_feature(Type controllerType, string featureCode)
    {
        var attribute = controllerType.GetCustomAttribute<RequireFeatureAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal(FeatureAuthorizationPolicies.For(featureCode), attribute!.Policy);
    }
}
