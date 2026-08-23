using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Billing.Dunning;
using Property.Domain.Entities;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class DunningAdminService : IDunningAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenant;

    public DunningAdminService(ApplicationDbContext db, ICurrentTenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<DunningRuleDto>> ListRulesAsync(
        Guid horizontalPropertyId,
        CancellationToken cancellationToken = default)
    {
        await EnsurePropertyAsync(horizontalPropertyId, cancellationToken);
        var rules = await _db.DunningRules.AsNoTracking()
            .Where(r => r.HorizontalPropertyId == horizontalPropertyId)
            .OrderBy(r => r.DaysOverdue)
            .ToListAsync(cancellationToken);
        return rules.Select(MapRule).ToList();
    }

    public async Task<DunningRuleDto> UpsertRuleAsync(
        UpsertDunningRuleDto dto,
        CancellationToken cancellationToken = default)
    {
        await EnsurePropertyAsync(dto.HorizontalPropertyId, cancellationToken);

        if (dto.DaysOverdue < 1)
            throw new InvalidOperationException("DaysOverdue must be at least 1.");
        if (dto.CooldownDays < 1)
            throw new InvalidOperationException("CooldownDays must be at least 1.");

        DunningRule entity;
        if (dto.Id is Guid id)
        {
            entity = await _db.DunningRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("Dunning rule was not found.");
            if (entity.HorizontalPropertyId != dto.HorizontalPropertyId)
                throw new InvalidOperationException("Rule does not belong to the horizontal property.");
        }
        else
        {
            var duplicate = await _db.DunningRules.AnyAsync(
                r => r.HorizontalPropertyId == dto.HorizontalPropertyId && r.DaysOverdue == dto.DaysOverdue,
                cancellationToken);
            if (duplicate)
                throw new InvalidOperationException("Ya existe una regla con esos días de mora.");

            entity = new DunningRule
            {
                Id = Guid.NewGuid(),
                TenantId = ResolveTenantId(),
                HorizontalPropertyId = dto.HorizontalPropertyId,
            };
            await _db.DunningRules.AddAsync(entity, cancellationToken);
        }

        entity.DaysOverdue = dto.DaysOverdue;
        entity.IsEnabled = dto.IsEnabled;
        entity.CooldownDays = dto.CooldownDays;
        entity.EmailSubjectTemplate = dto.EmailSubjectTemplate.Trim();
        entity.EmailBodyTemplate = dto.EmailBodyTemplate.Trim();
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (dto.Id.HasValue)
            _db.DunningRules.Update(entity);

        await _db.SaveChangesAsync(cancellationToken);
        return MapRule(entity);
    }

    public async Task<bool> DeleteRuleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.DunningRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (entity is null)
            return false;
        _db.DunningRules.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<DunningSendLogDto>> ListRecentSendsAsync(
        Guid horizontalPropertyId,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        await EnsurePropertyAsync(horizontalPropertyId, cancellationToken);
        take = Math.Clamp(take, 1, 200);

        var rows = await (
            from log in _db.DunningSendLogs.AsNoTracking()
            join u in _db.Units.AsNoTracking() on log.UnitId equals u.Id
            where log.HorizontalPropertyId == horizontalPropertyId
            orderby log.SentAtUtc descending
            select new DunningSendLogDto
            {
                Id = log.Id,
                HorizontalPropertyId = log.HorizontalPropertyId,
                UnitId = log.UnitId,
                UnitCode = u.Code,
                DunningRuleId = log.DunningRuleId,
                DaysOverdueSnapshot = log.DaysOverdueSnapshot,
                BalanceSnapshot = log.BalanceSnapshot,
                ToEmail = log.ToEmail,
                SentAtUtc = log.SentAtUtc,
            }
        ).Take(take).ToListAsync(cancellationToken);

        return rows;
    }

    private async Task EnsurePropertyAsync(Guid horizontalPropertyId, CancellationToken cancellationToken)
    {
        var exists = await _db.HorizontalProperties.AnyAsync(p => p.Id == horizontalPropertyId, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("Horizontal property was not found.");
    }

    private Guid ResolveTenantId()
    {
        if (_tenant.TenantId is { } tenantId && tenantId != Guid.Empty)
            return tenantId;
        throw new InvalidOperationException("Tenant context is not available.");
    }

    private static DunningRuleDto MapRule(DunningRule r) =>
        new()
        {
            Id = r.Id,
            HorizontalPropertyId = r.HorizontalPropertyId,
            DaysOverdue = r.DaysOverdue,
            IsEnabled = r.IsEnabled,
            CooldownDays = r.CooldownDays,
            EmailSubjectTemplate = r.EmailSubjectTemplate,
            EmailBodyTemplate = r.EmailBodyTemplate,
        };
}
