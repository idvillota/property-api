using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Options;
using Property.Infrastructure.Common;
using Property.Infrastructure.Identity;
using Property.Infrastructure.Persistence;
using Property.Infrastructure.Services;

namespace Property.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<PlatformOptions>(configuration.GetSection(PlatformOptions.SectionName));
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ICurrentTenantContext, CurrentTenantContext>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'ConnectionStrings:DefaultConnection' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IUserPreferencesService, UserPreferencesService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITenantUserInviteService, TenantUserInviteService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<ITenantSettingsService, TenantSettingsService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IRolePermissionService, RolePermissionService>();
        services.AddScoped<INeighborhoodService, NeighborhoodService>();
        services.AddScoped<IHorizontalPropertyService, HorizontalPropertyService>();
        services.AddScoped<IBuildingService, BuildingService>();
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<IVehicleBrandService, VehicleBrandService>();
        services.AddScoped<IFloorService, FloorService>();
        services.AddScoped<IUnitService, UnitService>();
        services.AddScoped<IStructureComposerService, StructureComposerService>();
        services.AddScoped<IPersonService, PersonService>();
        services.AddScoped<IUnitOccupancyService, UnitOccupancyService>();
        services.AddScoped<IUserPersonLinkService, UserPersonLinkService>();
        services.AddScoped<IFeeSettingsService, FeeSettingsService>();
        services.AddScoped<IBillingPeriodService, BillingPeriodService>();
        services.AddScoped<IFeeChargeService, FeeChargeService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<ICommonAreaService, CommonAreaService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IReceptionNotificationService, ReceptionNotificationService>();
        services.AddScoped<IReceptionItemService, ReceptionItemService>();
        services.AddScoped<IMuralPostService, MuralPostService>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
