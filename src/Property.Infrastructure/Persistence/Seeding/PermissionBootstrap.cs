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
                    // Keep stable catalog ids; recreate row if id drifted.
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
}
