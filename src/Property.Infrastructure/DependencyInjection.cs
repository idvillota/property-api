using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Options;
using Property.Infrastructure.Auditing;
using Property.Infrastructure.Common;
using Property.Infrastructure.Identity;
using Property.Infrastructure.Messaging;
using Property.Infrastructure.Payments;
using Property.Infrastructure.Persistence;
using Property.Infrastructure.Services;
using Property.Infrastructure.Storage;

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
        services.AddScoped<ITenantModuleService, TenantModuleService>();
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
        services.AddScoped<IPortalService, PortalService>();
        services.AddScoped<IFeeSettingsService, FeeSettingsService>();
        services.AddScoped<IBillingPeriodService, BillingPeriodService>();
        services.AddScoped<IFeeChargeService, FeeChargeService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IPaymentIntentService, PaymentIntentService>();
        services.Configure<WompiOptions>(configuration.GetSection(WompiOptions.SectionName));
        services.Configure<DunningOptions>(configuration.GetSection(DunningOptions.SectionName));
        services.AddScoped<IPspCheckoutService, WompiPspCheckoutService>();
        services.AddScoped<IPspWebhookService, WompiWebhookService>();
        services.AddScoped<IDunningAdminService, DunningAdminService>();
        services.AddScoped<IDunningJobService, DunningJobService>();
        services.AddScoped<ICommonAreaService, CommonAreaService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        RegisterFileStorage(services, configuration);
        services.AddScoped<IFileAssetService, FileAssetService>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<IOutboxDispatcher, OutboxDispatcher>();
        services.AddScoped<IOutboxMessageHandler, EmailSendOutboxHandler>();
        services.AddScoped<IReceptionNotificationService, ReceptionNotificationService>();
        services.AddScoped<IReceptionItemService, ReceptionItemService>();
        services.AddScoped<IVisitLogService, VisitLogService>();
        services.AddScoped<IMuralPostService, MuralPostService>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }

    private static void RegisterFileStorage(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetSection(FileStorageOptions.SectionName).GetValue<string>("Provider")
            ?? "Local";
        if (string.Equals(provider, "S3", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IFileStorage, S3CompatibleFileStorage>();
        else
            services.AddSingleton<IFileStorage, LocalDiskFileStorage>();
    }
}
