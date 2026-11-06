using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Billing.BreB;
using Property.Domain.Entities;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class BreBPaymentProfileService : IBreBPaymentProfileService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public BreBPaymentProfileService(ApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<BreBPaymentProfileDto> GetOrCreateAsync(
        Guid horizontalPropertyId,
        CancellationToken cancellationToken = default)
    {
        var property = await _db.HorizontalProperties
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == horizontalPropertyId, cancellationToken)
            ?? throw new InvalidOperationException("Horizontal property was not found.");

        var profile = await _db.BreBPaymentProfiles
            .FirstOrDefaultAsync(p => p.HorizontalPropertyId == horizontalPropertyId, cancellationToken);
        if (profile is null)
        {
            profile = new BreBPaymentProfile
            {
                HorizontalPropertyId = horizontalPropertyId,
                TenantId = ResolveTenantId(),
                MerchantName = FirstText(property.TradeName, property.Name),
                City = TrimTo(property.City, 40),
                PostalCode = TrimTo(DigitsAndLetters(property.PostalCode), 10),
            };
            await _db.BreBPaymentProfiles.AddAsync(profile, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Map(profile);
    }

    public async Task<BreBPaymentProfileDto?> UpdateAsync(
        Guid horizontalPropertyId,
        UpdateBreBPaymentProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        var propertyExists = await _db.HorizontalProperties
            .AnyAsync(p => p.Id == horizontalPropertyId, cancellationToken);
        if (!propertyExists)
            return null;

        var key = dto.Key.Trim();
        var merchant = TrimTo(dto.MerchantName, 80);
        var city = TrimTo(dto.City, 40);
        var postal = TrimTo(DigitsAndLetters(dto.PostalCode), 10);

        if (key.Length > 40 || key.Any(static c => c < 33 || c > 126))
        {
            throw new InvalidOperationException(
                "La llave Bre-B debe tener entre 1 y 40 caracteres simples, sin espacios.");
        }

        if (key.Length > 0 && dto.KeyType is < 1 or > 5)
            throw new InvalidOperationException("Elige el tipo de llave Bre-B.");

        if (dto.KeyType is < 0 or > 5)
            throw new InvalidOperationException("El tipo de llave Bre-B no es válido.");

        var profile = await _db.BreBPaymentProfiles
            .FirstOrDefaultAsync(p => p.HorizontalPropertyId == horizontalPropertyId, cancellationToken);
        if (profile is null)
        {
            profile = new BreBPaymentProfile
            {
                HorizontalPropertyId = horizontalPropertyId,
                TenantId = ResolveTenantId(),
            };
            await _db.BreBPaymentProfiles.AddAsync(profile, cancellationToken);
        }

        profile.Key = key;
        profile.KeyType = dto.KeyType;
        profile.MerchantName = merchant;
        profile.City = city;
        profile.PostalCode = postal;
        await _db.SaveChangesAsync(cancellationToken);
        return Map(profile);
    }

    private static string FirstText(string? preferred, string fallback) =>
        TrimTo(string.IsNullOrWhiteSpace(preferred) ? fallback : preferred, 80);

    private static string TrimTo(string? value, int max)
    {
        var text = (value ?? string.Empty).Trim();
        return text.Length <= max ? text : text[..max];
    }

    private static string DigitsAndLetters(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        return new string(value.Where(static c => char.IsLetterOrDigit(c) && c < 128).ToArray());
    }

    private Guid ResolveTenantId()
    {
        if (_tenantContext.TenantId is { } tenantId && tenantId != Guid.Empty)
            return tenantId;
        throw new InvalidOperationException("Tenant context is not available.");
    }

    private static BreBPaymentProfileDto Map(BreBPaymentProfile profile)
    {
        var qr = new BreBQrProfile(
            profile.Key,
            profile.KeyType,
            profile.MerchantName,
            profile.City,
            profile.PostalCode);
        return new BreBPaymentProfileDto
        {
            HorizontalPropertyId = profile.HorizontalPropertyId,
            Key = profile.Key,
            KeyType = profile.KeyType,
            MerchantName = profile.MerchantName,
            City = profile.City,
            PostalCode = profile.PostalCode,
            IsReady = BreBQrPayload.ConfigurationError(qr) is null,
        };
    }
}
