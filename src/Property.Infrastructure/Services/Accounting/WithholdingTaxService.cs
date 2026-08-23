using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Accounting;
using Property.Domain.Entities;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services.Accounting;

public sealed class WithholdingTaxService : IWithholdingTaxService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenant;

    public WithholdingTaxService(ApplicationDbContext db, ICurrentTenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<WithholdingTaxRateDto>> ListRatesAsync(
        CancellationToken cancellationToken = default) =>
        await _db.WithholdingTaxRates.AsNoTracking().OrderBy(r => r.Kind)
            .Select(r => Map(r)).ToListAsync(cancellationToken);

    public async Task<WithholdingTaxRateDto> UpsertRateAsync(
        UpsertWithholdingTaxRateDto dto, CancellationToken cancellationToken = default)
    {
        var existing = await _db.WithholdingTaxRates
            .FirstOrDefaultAsync(r => r.Kind == dto.Kind, cancellationToken);
        if (existing is null)
        {
            existing = new WithholdingTaxRate
            {
                Id = Guid.NewGuid(),
                TenantId = RequireTenant(),
                Kind = dto.Kind,
                CreatedAtUtc = DateTime.UtcNow,
            };
            await _db.WithholdingTaxRates.AddAsync(existing, cancellationToken);
        }

        existing.Name = dto.Name.Trim();
        existing.RatePercent = Math.Round(dto.RatePercent, 4, MidpointRounding.AwayFromZero);
        existing.MinimumBase = Math.Round(dto.MinimumBase, 2, MidpointRounding.AwayFromZero);
        existing.IsActive = dto.IsActive;
        existing.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Map(existing);
    }

    public async Task<IReadOnlyList<WithholdingExportRowDto>> ExportAsync(
        Guid horizontalPropertyId, int year, int? month = null, CancellationToken cancellationToken = default)
    {
        var q = _db.ApInvoices.AsNoTracking()
            .Include(i => i.Vendor)
            .Where(i => i.HorizontalPropertyId == horizontalPropertyId
                        && i.IsActive
                        && i.WithholdingAmount > 0
                        && i.IssueDate.Year == year);
        if (month.HasValue)
            q = q.Where(i => i.IssueDate.Month == month.Value);

        var list = await q.OrderBy(i => i.IssueDate).ToListAsync(cancellationToken);
        return list.Select(i => new WithholdingExportRowDto
        {
            ApInvoiceId = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            VendorName = i.Vendor?.Name ?? string.Empty,
            Kind = i.WithholdingKind ?? Domain.Enums.WithholdingKind.ReteFte,
            BaseAmount = i.Subtotal,
            WithholdingAmount = i.WithholdingAmount,
            IssueDate = i.IssueDate,
        }).ToList();
    }

    private Guid RequireTenant() =>
        _tenant.TenantId is Guid id && id != Guid.Empty
            ? id
            : throw new InvalidOperationException("Tenant context is required.");

    private static WithholdingTaxRateDto Map(WithholdingTaxRate r) => new()
    {
        Id = r.Id,
        Kind = r.Kind,
        Name = r.Name,
        RatePercent = r.RatePercent,
        MinimumBase = r.MinimumBase,
        IsActive = r.IsActive,
    };
}
