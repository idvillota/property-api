using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Property.Domain.Entities;
using Property.Infrastructure.Authorization;

namespace Property.Infrastructure.Persistence.Seeding;

public static class PermissionBootstrap
{
    public static async Task EnsureAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await SyncFeatureCatalogAsync(db, logger, cancellationToken);
        await EnsureSystemRolesAsync(db, logger, cancellationToken);
        await GrantMissingDefaultRoleFeaturesAsync(db, logger, cancellationToken);
    }

    private static async Task EnsureSystemRolesAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var tenants = await db.Tenants.AsNoTracking().Select(t => t.Id).ToListAsync(cancellationToken);
        if (tenants.Count == 0)
            return;

        var existing = await db.Roles
            .IgnoreQueryFilters()
            .Where(r => tenants.Contains(r.TenantId) && SystemRoles.All.Contains(r.Name))
            .Select(r => new { r.TenantId, r.Name })
            .ToListAsync(cancellationToken);
        var existingSet = existing
            .Select(x => (x.TenantId, x.Name))
            .ToHashSet();

        var added = 0;
        foreach (var tenantId in tenants)
        {
            foreach (var roleName in SystemRoles.All)
            {
                if (existingSet.Contains((tenantId, roleName)))
                    continue;

                await db.Roles.AddAsync(
                    new Role
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        Name = roleName,
                        NormalizedName = roleName.ToUpperInvariant(),
                    },
                    cancellationToken);
                existingSet.Add((tenantId, roleName));
                added++;
            }
        }

        if (added == 0)
            return;

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created {Count} missing system role(s).", added);
    }

    private static async Task SyncFeatureCatalogAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var existing = await db.Features.AsNoTracking().ToListAsync(cancellationToken);
        var byCode = existing.ToDictionary(f => f.Code, StringComparer.Ordinal);
        var catalogCodes = FeatureCatalog.All.Select(f => f.Code).ToHashSet(StringComparer.Ordinal);
        var changed = false;

        foreach (var def in FeatureCatalog.All)
        {
            if (byCode.TryGetValue(def.Code, out var feature))
            {
                var tracked = await db.Features.FirstAsync(f => f.Id == feature.Id, cancellationToken);
                if (tracked.Name != def.Name
                    || tracked.Module != def.Module
                    || tracked.SortOrder != def.SortOrder
                    || tracked.Id != def.Id)
                {
                    if (tracked.Id != def.Id)
                    {
                        db.Features.Remove(tracked);
                        await db.Features.AddAsync(
                            new Feature
                            {
                                Id = def.Id,
                                Code = def.Code,
                                Name = def.Name,
                                Module = def.Module,
                                SortOrder = def.SortOrder,
                            },
                            cancellationToken);
                    }
                    else
                    {
                        tracked.Name = def.Name;
                        tracked.Module = def.Module;
                        tracked.SortOrder = def.SortOrder;
                    }

                    changed = true;
                }

                continue;
            }

            await db.Features.AddAsync(
                new Feature
                {
                    Id = def.Id,
                    Code = def.Code,
                    Name = def.Name,
                    Module = def.Module,
                    SortOrder = def.SortOrder,
                },
                cancellationToken);
            changed = true;
        }

        var obsolete = existing.Where(f => !catalogCodes.Contains(f.Code)).ToList();
        if (obsolete.Count > 0)
        {
            var obsoleteIds = obsolete.Select(f => f.Id).ToHashSet();
            var roleFeatures = await db.RoleFeatures
                .IgnoreQueryFilters()
                .Where(rf => obsoleteIds.Contains(rf.FeatureId))
                .ToListAsync(cancellationToken);
            if (roleFeatures.Count > 0)
                db.RoleFeatures.RemoveRange(roleFeatures);

            var trackedObsolete = await db.Features
                .Where(f => obsoleteIds.Contains(f.Id))
                .ToListAsync(cancellationToken);
            db.Features.RemoveRange(trackedObsolete);
            changed = true;
            logger.LogInformation(
                "Removed {Count} obsolete feature(s): {Codes}",
                obsolete.Count,
                string.Join(", ", obsolete.Select(f => f.Code)));
        }

        if (!changed)
        {
            logger.LogDebug("Feature catalog is already up to date.");
            return;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Feature catalog synchronized ({Count} features).", FeatureCatalog.All.Length);
    }

    /// <summary>
    /// Grants missing default features to system roles (Administrator/Owner/Manager)
    /// so new Wave features appear without manual role edits.
    /// </summary>
    private static async Task GrantMissingDefaultRoleFeaturesAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var featuresByCode = await db.Features
            .AsNoTracking()
            .ToDictionaryAsync(f => f.Code, f => f.Id, StringComparer.Ordinal, cancellationToken);

        var roles = await db.Roles
            .IgnoreQueryFilters()
            .Where(r =>
                r.NormalizedName == SystemRoles.Administrator.ToUpperInvariant()
                || r.NormalizedName == SystemRoles.Owner.ToUpperInvariant()
                || r.NormalizedName == SystemRoles.Manager.ToUpperInvariant()
                || r.NormalizedName == SystemRoles.Receptionist.ToUpperInvariant())
            .ToListAsync(cancellationToken);

        if (roles.Count == 0)
            return;

        var roleIds = roles.Select(r => r.Id).ToList();
        var existingLinks = await db.RoleFeatures
            .IgnoreQueryFilters()
            .Where(rf => roleIds.Contains(rf.RoleId))
            .Select(rf => new { rf.RoleId, rf.FeatureId })
            .ToListAsync(cancellationToken);
        var existingSet = existingLinks
            .Select(x => (x.RoleId, x.FeatureId))
            .ToHashSet();

        var added = 0;
        foreach (var role in roles)
        {
            if (!FeatureCatalog.DefaultFeaturesByRole.TryGetValue(role.Name, out var codes))
                continue;

            foreach (var code in codes)
            {
                if (!featuresByCode.TryGetValue(code, out var featureId))
                    continue;
                if (existingSet.Contains((role.Id, featureId)))
                    continue;

                await db.RoleFeatures.AddAsync(
                    new RoleFeature
                    {
                        Id = Guid.NewGuid(),
                        TenantId = role.TenantId,
                        RoleId = role.Id,
                        FeatureId = featureId,
                    },
                    cancellationToken);
                existingSet.Add((role.Id, featureId));
                added++;
            }
        }

        if (added == 0)
            return;

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Granted {Count} missing default role feature link(s).", added);
    }
}
