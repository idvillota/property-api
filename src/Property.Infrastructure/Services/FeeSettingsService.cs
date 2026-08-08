using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Billing.FeeSettings;
using Property.Domain.Entities;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class FeeSettingsService : IFeeSettingsService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public FeeSettingsService(ApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<FeeSettingsDto> GetOrCreateAsync(
        Guid horizontalPropertyId,
        CancellationToken cancellationToken = default)
    {
        await EnsurePropertyExistsAsync(horizontalPropertyId, cancellationToken);
        var settings = await GetOrCreateSettingsAsync(horizontalPropertyId, cancellationToken);
        return Map(settings);
    }

    public async Task<FeeSettingsDto?> UpdateAsync(
        Guid horizontalPropertyId,
        UpdateFeeSettingsDto dto,
        CancellationToken cancellationToken = default)
    {
        var propertyExists = await _db.HorizontalProperties.AnyAsync(p => p.Id == horizontalPropertyId, cancellationToken);
        if (!propertyExists)
            return null;

        if (dto.DueDayOfMonth is < 1 or > 28)
            throw new InvalidOperationException("DueDayOfMonth must be between 1 and 28.");

        var settings = await GetOrCreateSettingsAsync(horizontalPropertyId, cancellationToken);
        settings.MonthlyBudgetAmount = dto.MonthlyBudgetAmount;
        settings.DueDayOfMonth = dto.DueDayOfMonth;
        settings.LateInterestMonthlyPercent = dto.LateInterestMonthlyPercent;
        settings.Notes = dto.Notes?.Trim();

        _db.FeeSettings.Update(settings);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(settings);
    }

    private async Task EnsurePropertyExistsAsync(Guid horizontalPropertyId, CancellationToken cancellationToken)
    {
        var exists = await _db.HorizontalProperties.AnyAsync(p => p.Id == horizontalPropertyId, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("Horizontal property was not found.");
    }

    private async Task<FeeSettings> GetOrCreateSettingsAsync(
        Guid horizontalPropertyId,
        CancellationToken cancellationToken)
    {
        var settings = await _db.FeeSettings
            .FirstOrDefaultAsync(s => s.HorizontalPropertyId == horizontalPropertyId, cancellationToken);
        if (settings is not null)
            return settings;

        settings = new FeeSettings
        {
            HorizontalPropertyId = horizontalPropertyId,
            TenantId = ResolveTenantId(),
            MonthlyBudgetAmount = 0m,
            DueDayOfMonth = 5,
            LateInterestMonthlyPercent = 0m,
        };
        await _db.FeeSettings.AddAsync(settings, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private Guid ResolveTenantId()
    {
        if (_tenantContext.TenantId is { } tenantId && tenantId != Guid.Empty)
            return tenantId;
        throw new InvalidOperationException("Tenant context is not available.");
    }

    private static FeeSettingsDto Map(FeeSettings settings) =>
        new()
        {
            HorizontalPropertyId = settings.HorizontalPropertyId,
            MonthlyBudgetAmount = settings.MonthlyBudgetAmount,
            DueDayOfMonth = settings.DueDayOfMonth,
            LateInterestMonthlyPercent = settings.LateInterestMonthlyPercent,
            Notes = settings.Notes,
        };
}
