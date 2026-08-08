using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Dashboard;
using Property.Domain.Entities;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class DashboardService : IDashboardService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public DashboardService(ApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<DashboardLayoutDto> GetLayoutAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateSettingsAsync(cancellationToken);
        return DeserializeLayout(settings.DashboardLayoutJson) ?? DefaultDashboardLayout.Create();
    }

    public async Task<DashboardLayoutDto> UpdateLayoutAsync(
        DashboardLayoutDto layout,
        CancellationToken cancellationToken = default)
    {
        ValidateLayout(layout);

        var settings = await GetOrCreateSettingsAsync(cancellationToken);
        settings.DashboardLayoutJson = JsonSerializer.Serialize(layout, JsonOptions);
        _db.TenantSettings.Update(settings);
        await _db.SaveChangesAsync(cancellationToken);
        return layout;
    }

    public IReadOnlyList<DashboardWidgetDefinitionDto> GetCatalog() => DashboardCatalog.Widgets;

    private static void ValidateLayout(DashboardLayoutDto layout)
    {
        if (layout.Panels.Count > 24)
            throw new InvalidOperationException("The dashboard allows at most 24 widgets.");

        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var panel in layout.Panels)
        {
            if (string.IsNullOrWhiteSpace(panel.Id))
                throw new InvalidOperationException("Each widget must have an identifier.");

            if (!seenIds.Add(panel.Id))
                throw new InvalidOperationException($"Duplicate widget identifier: {panel.Id}.");

            if (!DashboardCatalog.ByType.TryGetValue(panel.WidgetType, out var definition))
                throw new InvalidOperationException($"Unknown widget type: {panel.WidgetType}.");

            if (panel.W < definition.MinWidth || panel.H < definition.MinHeight)
                throw new InvalidOperationException(
                    $"Widget '{definition.Name}' requires at least {definition.MinWidth}×{definition.MinHeight} cells.");

            if (panel.X < 0 || panel.Y < 0 || panel.W < 1 || panel.H < 1)
                throw new InvalidOperationException("Invalid widget position or size.");

            if (panel.X + panel.W > 12)
                throw new InvalidOperationException($"Widget '{definition.Name}' exceeds the dashboard width.");
        }
    }

    private static DashboardLayoutDto? DeserializeLayout(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<DashboardLayoutDto>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<TenantSettings> GetOrCreateSettingsAsync(CancellationToken cancellationToken)
    {
        var tenantId = ResolveTenantId();
        var settings = await _db.TenantSettings.FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);
        if (settings is not null)
            return settings;

        settings = new TenantSettings
        {
            TenantId = tenantId,
        };
        await _db.TenantSettings.AddAsync(settings, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private Guid ResolveTenantId()
    {
        if (_tenantContext.TenantId is { } tenantId && tenantId != Guid.Empty)
            return tenantId;
        throw new InvalidOperationException("Tenant context is not available.");
    }
}
