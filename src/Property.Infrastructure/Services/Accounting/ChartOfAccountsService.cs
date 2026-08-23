using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Accounting;
using Property.Domain.Entities;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services.Accounting;

public sealed class ChartOfAccountsService : IChartOfAccountsService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenant;

    public ChartOfAccountsService(ApplicationDbContext db, ICurrentTenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenant();
        var existing = await _db.LedgerAccounts.IgnoreQueryFilters()
            .Where(a => a.TenantId == tenantId)
            .Select(a => a.Code)
            .ToListAsync(cancellationToken);
        var existingSet = existing.ToHashSet(StringComparer.Ordinal);
        var now = DateTime.UtcNow;
        foreach (var (code, name, type, parent, postable) in PucSeed.Accounts)
        {
            if (existingSet.Contains(code))
                continue;
            await _db.LedgerAccounts.AddAsync(new LedgerAccount
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Code = code,
                Name = name,
                AccountType = type,
                ParentCode = parent,
                IsPostable = postable,
                IsSystem = true,
                IsActive = true,
                CreatedAtUtc = now,
            }, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LedgerAccountDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        var rows = await _db.LedgerAccounts.AsNoTracking()
            .OrderBy(a => a.Code)
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<LedgerAccountDto?> UpdateAsync(
        Guid id, UpdateLedgerAccountDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _db.LedgerAccounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (entity is null)
            return null;
        entity.Name = dto.Name.Trim();
        entity.IsActive = dto.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    private Guid RequireTenant()
    {
        if (_tenant.TenantId is not Guid id || id == Guid.Empty)
            throw new InvalidOperationException("Tenant context is required.");
        return id;
    }

    private static LedgerAccountDto Map(LedgerAccount a) => new()
    {
        Id = a.Id,
        Code = a.Code,
        Name = a.Name,
        AccountType = a.AccountType,
        ParentCode = a.ParentCode,
        IsPostable = a.IsPostable,
        IsSystem = a.IsSystem,
        IsActive = a.IsActive,
    };
}
