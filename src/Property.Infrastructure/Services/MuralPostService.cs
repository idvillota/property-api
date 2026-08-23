using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Community.Mural;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Common;

namespace Property.Infrastructure.Services;

public sealed class MuralPostService : IMuralPostService
{
    private readonly IRepository<MuralPost> _posts;
    private readonly IRepository<MuralPostRead> _reads;
    private readonly IRepository<HorizontalProperty> _properties;
    private readonly IRepository<Unit> _units;
    private readonly IRepository<Building> _buildings;
    private readonly IRepository<Floor> _floors;
    private readonly IRepository<UnitOccupancy> _occupancies;
    private readonly IRepository<User> _users;
    private readonly ICurrentTenantContext _tenant;
    private readonly IUnitOfWork _unitOfWork;

    public MuralPostService(
        IRepository<MuralPost> posts,
        IRepository<MuralPostRead> reads,
        IRepository<HorizontalProperty> properties,
        IRepository<Unit> units,
        IRepository<Building> buildings,
        IRepository<Floor> floors,
        IRepository<UnitOccupancy> occupancies,
        IRepository<User> users,
        ICurrentTenantContext tenant,
        IUnitOfWork unitOfWork)
    {
        _posts = posts;
        _reads = reads;
        _properties = properties;
        _units = units;
        _buildings = buildings;
        _floors = floors;
        _occupancies = occupancies;
        _users = users;
        _tenant = tenant;
        _unitOfWork = unitOfWork;
    }

    public Task<PagedResult<MuralPostDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default) =>
        ListQueryHelpers.ToPagedResultAsync(
            _posts.Query().AsNoTracking(),
            query,
            q => PagedEntityQueries.ShapeMuralPosts(q, query),
            async entities =>
            {
                var unitIds = entities.Where(e => e.UnitId.HasValue).Select(e => e.UnitId!.Value).Distinct().ToList();
                var unitCodes = unitIds.Count == 0
                    ? new Dictionary<Guid, string>()
                    : await _units.Query().AsNoTracking()
                        .Where(u => unitIds.Contains(u.Id))
                        .ToDictionaryAsync(u => u.Id, u => u.Code, cancellationToken);
                var stats = await BuildReadStatsAsync(entities.ToList(), cancellationToken);
                IReadOnlyList<MuralPostDto> items = entities.Select(e =>
                {
                    var dto = Map(e, unitCodes);
                    if (stats.TryGetValue(e.Id, out var s))
                    {
                        dto.ReadCount = s.ReadCount;
                        dto.RecipientCount = s.RecipientCount;
                        dto.ReadPercent = s.ReadPercent;
                    }
                    return dto;
                }).ToList();
                return items;
            },
            cancellationToken);

    public async Task<IReadOnlyList<MuralPostDto>> ListPublicAsync(
        Guid? horizontalPropertyId = null,
        MuralReaderScope? readerScope = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var query = _posts.Query().AsNoTracking()
            .Where(e =>
                e.IsActive &&
                !e.IsHidden &&
                (e.ExpiresAtUtc == null || e.ExpiresAtUtc >= now));

        if (horizontalPropertyId.HasValue)
            query = query.Where(e => e.HorizontalPropertyId == horizontalPropertyId.Value);

        var entities = await query
            .OrderByDescending(e => e.IsPinned)
            .ThenByDescending(e => e.PublishedAtUtc)
            .Take(300)
            .ToListAsync(cancellationToken);

        if (readerScope is not null)
        {
            entities = entities
                .Where(e => MuralAudience.MatchesReader(
                    MuralAudience.ParseGuids(e.AudienceBuildingIdsJson),
                    MuralAudience.ParseGuids(e.AudienceFloorIdsJson),
                    MuralAudience.ParseRoles(e.AudienceOccupancyRolesJson),
                    readerScope))
                .Take(100)
                .ToList();
        }
        else
        {
            entities = entities.Take(100).ToList();
        }

        var unitIds = entities.Where(e => e.UnitId.HasValue).Select(e => e.UnitId!.Value).Distinct().ToList();
        var unitCodes = unitIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _units.Query().AsNoTracking()
                .Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Code, cancellationToken);

        return entities.Select(e => Map(e, unitCodes)).ToList();
    }

    public async Task<MuralPostDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _posts.Query().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
            return null;

        string? unitCode = null;
        if (entity.UnitId is { } unitId)
            unitCode = await _units.Query().AsNoTracking()
                .Where(u => u.Id == unitId)
                .Select(u => u.Code)
                .FirstOrDefaultAsync(cancellationToken);

        var codes = unitCode is null || entity.UnitId is null
            ? new Dictionary<Guid, string>()
            : new Dictionary<Guid, string> { [entity.UnitId.Value] = unitCode };
        var dto = Map(entity, codes);
        var stats = await BuildReadStatsAsync(new[] { entity }, cancellationToken);
        if (stats.TryGetValue(entity.Id, out var s))
        {
            dto.ReadCount = s.ReadCount;
            dto.RecipientCount = s.RecipientCount;
            dto.ReadPercent = s.ReadPercent;
        }
        return dto;
    }

    public async Task<MuralPostDto> CreateAsync(CreateMuralPostDto dto, CancellationToken cancellationToken = default)
    {
        await EnsurePropertyAsync(dto.HorizontalPropertyId, cancellationToken);
        await EnsureUnitAsync(dto.HorizontalPropertyId, dto.UnitId, cancellationToken);
        await EnsureAudienceAsync(
            dto.HorizontalPropertyId,
            dto.AudienceBuildingIds,
            dto.AudienceFloorIds,
            cancellationToken);
        ValidateExpiry(dto.ExpiresAtUtc);

        if (dto.Kind == MuralPostKind.Community && dto.IsPinned)
            throw new InvalidOperationException("Solo los avisos oficiales se pueden fijar.");

        var now = DateTime.UtcNow;
        var authorName = await ResolveAuthorDisplayNameAsync(cancellationToken);

        var entity = new MuralPost
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = dto.HorizontalPropertyId,
            Kind = dto.Kind,
            Category = dto.Category,
            Title = dto.Title.Trim(),
            Body = dto.Body.Trim(),
            ContactPhone = NullIfEmpty(dto.ContactPhone),
            ContactEmail = NullIfEmpty(dto.ContactEmail),
            ContactNote = NullIfEmpty(dto.ContactNote),
            AreaLabel = NullIfEmpty(dto.AreaLabel),
            UnitId = dto.UnitId,
            AuthorUserId = _tenant.UserId,
            AuthorDisplayName = authorName,
            IsPinned = dto.Kind == MuralPostKind.Official && dto.IsPinned,
            IsHidden = false,
            PublishedAtUtc = now,
            ExpiresAtUtc = dto.ExpiresAtUtc?.ToUniversalTime(),
            IsActive = true,
            AudienceBuildingIdsJson = MuralAudience.SerializeGuids(dto.AudienceBuildingIds),
            AudienceFloorIdsJson = MuralAudience.SerializeGuids(dto.AudienceFloorIds),
            AudienceOccupancyRolesJson = MuralAudience.SerializeRoles(dto.AudienceOccupancyRoles),
        };

        await _posts.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<MuralPostDto?> UpdateAsync(Guid id, UpdateMuralPostDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _posts.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        await EnsurePropertyAsync(dto.HorizontalPropertyId, cancellationToken);
        await EnsureUnitAsync(dto.HorizontalPropertyId, dto.UnitId, cancellationToken);
        await EnsureAudienceAsync(
            dto.HorizontalPropertyId,
            dto.AudienceBuildingIds,
            dto.AudienceFloorIds,
            cancellationToken);
        ValidateExpiry(dto.ExpiresAtUtc);

        if (dto.Kind == MuralPostKind.Community && dto.IsPinned)
            throw new InvalidOperationException("Solo los avisos oficiales se pueden fijar.");

        entity.HorizontalPropertyId = dto.HorizontalPropertyId;
        entity.Kind = dto.Kind;
        entity.Category = dto.Category;
        entity.Title = dto.Title.Trim();
        entity.Body = dto.Body.Trim();
        entity.ContactPhone = NullIfEmpty(dto.ContactPhone);
        entity.ContactEmail = NullIfEmpty(dto.ContactEmail);
        entity.ContactNote = NullIfEmpty(dto.ContactNote);
        entity.AreaLabel = NullIfEmpty(dto.AreaLabel);
        entity.UnitId = dto.UnitId;
        entity.IsPinned = dto.Kind == MuralPostKind.Official && dto.IsPinned;
        entity.IsHidden = dto.IsHidden;
        entity.ExpiresAtUtc = dto.ExpiresAtUtc?.ToUniversalTime();
        entity.IsActive = dto.IsActive;
        entity.AudienceBuildingIdsJson = MuralAudience.SerializeGuids(dto.AudienceBuildingIds);
        entity.AudienceFloorIdsJson = MuralAudience.SerializeGuids(dto.AudienceFloorIds);
        entity.AudienceOccupancyRolesJson = MuralAudience.SerializeRoles(dto.AudienceOccupancyRoles);
        _posts.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _posts.GetByIdAsync(id, cancellationToken);
        if (entity is null || !entity.IsActive)
            return false;

        entity.IsActive = false;
        entity.IsPinned = false;
        _posts.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<MuralAudiencePreviewDto> PreviewAudienceAsync(
        MuralAudiencePreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsurePropertyAsync(request.HorizontalPropertyId, cancellationToken);
        await EnsureAudienceAsync(
            request.HorizontalPropertyId,
            request.AudienceBuildingIds,
            request.AudienceFloorIds,
            cancellationToken);

        var buildingIds = (request.AudienceBuildingIds ?? Array.Empty<Guid>()).Distinct().ToList();
        var floorIds = (request.AudienceFloorIds ?? Array.Empty<Guid>()).Distinct().ToList();
        var roles = (request.AudienceOccupancyRoles ?? Array.Empty<OccupancyRole>()).Distinct().ToList();
        var isBroadcast = MuralAudience.IsBroadcast(buildingIds, floorIds, roles);

        var unitsQuery = _units.Query().AsNoTracking()
            .Where(u => u.HorizontalPropertyId == request.HorizontalPropertyId && u.IsActive);

        if (buildingIds.Count > 0)
            unitsQuery = unitsQuery.Where(u => u.BuildingId != null && buildingIds.Contains(u.BuildingId.Value));

        if (floorIds.Count > 0)
            unitsQuery = unitsQuery.Where(u => u.FloorId != null && floorIds.Contains(u.FloorId.Value));

        var matchingUnitIds = await unitsQuery.Select(u => u.Id).ToListAsync(cancellationToken);
        var matchingUnitCount = matchingUnitIds.Count;

        var occQuery = _occupancies.Query().AsNoTracking()
            .Where(o => o.IsActive && matchingUnitIds.Contains(o.UnitId));

        if (roles.Count > 0)
            occQuery = occQuery.Where(o => roles.Contains(o.Role));

        var matchingOccupancyCount = await occQuery.CountAsync(cancellationToken);

        return new MuralAudiencePreviewDto
        {
            MatchingUnitCount = matchingUnitCount,
            MatchingOccupancyCount = matchingOccupancyCount,
            IsBroadcast = isBroadcast,
            Summary = MuralAudience.BuildSummary(
                buildingIds,
                floorIds,
                roles,
                matchingUnitCount,
                matchingOccupancyCount),
        };
    }

    public async Task<MuralPostReadDto> MarkReadAsync(
        Guid muralPostId,
        Guid personId,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var postExists = await _posts.Query().AsNoTracking()
            .AnyAsync(p => p.Id == muralPostId && p.IsActive, cancellationToken);
        if (!postExists)
            throw new InvalidOperationException("No encontramos el aviso.");

        var existing = await _reads.Query()
            .FirstOrDefaultAsync(r => r.MuralPostId == muralPostId && r.PersonId == personId, cancellationToken);

        if (existing is not null)
        {
            return new MuralPostReadDto
            {
                MuralPostId = muralPostId,
                PersonId = personId,
                ReadAtUtc = existing.ReadAtUtc,
                AlreadyRead = true,
            };
        }

        var now = DateTime.UtcNow;
        var entity = new MuralPostRead
        {
            Id = Guid.NewGuid(),
            MuralPostId = muralPostId,
            PersonId = personId,
            UserId = userId,
            ReadAtUtc = now,
        };
        await _reads.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MuralPostReadDto
        {
            MuralPostId = muralPostId,
            PersonId = personId,
            ReadAtUtc = now,
            AlreadyRead = false,
        };
    }

    public async Task<MuralReadStatsDto?> GetReadStatsAsync(
        Guid muralPostId,
        CancellationToken cancellationToken = default)
    {
        var post = await _posts.Query().AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == muralPostId, cancellationToken);
        if (post is null)
            return null;

        var stats = await BuildReadStatsAsync(new[] { post }, cancellationToken);
        return stats.TryGetValue(muralPostId, out var s)
            ? s
            : new MuralReadStatsDto { MuralPostId = muralPostId };
    }

    private async Task<Dictionary<Guid, MuralReadStatsDto>> BuildReadStatsAsync(
        IReadOnlyList<MuralPost> posts,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, MuralReadStatsDto>();
        if (posts.Count == 0)
            return result;

        var postIds = posts.Select(p => p.Id).ToList();
        var readCounts = await _reads.Query().AsNoTracking()
            .Where(r => postIds.Contains(r.MuralPostId))
            .GroupBy(r => r.MuralPostId)
            .Select(g => new { PostId = g.Key, Count = g.Select(x => x.PersonId).Distinct().Count() })
            .ToListAsync(cancellationToken);
        var readByPost = readCounts.ToDictionary(x => x.PostId, x => x.Count);

        foreach (var post in posts)
        {
            var recipientCount = await CountRecipientsAsync(post, cancellationToken);
            var readCount = readByPost.GetValueOrDefault(post.Id);
            var percent = recipientCount <= 0
                ? 0m
                : Math.Round(100m * readCount / recipientCount, 1, MidpointRounding.AwayFromZero);
            result[post.Id] = new MuralReadStatsDto
            {
                MuralPostId = post.Id,
                ReadCount = readCount,
                RecipientCount = recipientCount,
                ReadPercent = percent,
            };
        }

        return result;
    }

    private async Task<int> CountRecipientsAsync(MuralPost post, CancellationToken cancellationToken)
    {
        var buildingIds = MuralAudience.ParseGuids(post.AudienceBuildingIdsJson);
        var floorIds = MuralAudience.ParseGuids(post.AudienceFloorIdsJson);
        var roles = MuralAudience.ParseRoles(post.AudienceOccupancyRolesJson);

        var unitsQuery = _units.Query().AsNoTracking()
            .Where(u => u.HorizontalPropertyId == post.HorizontalPropertyId && u.IsActive);

        if (buildingIds.Count > 0)
            unitsQuery = unitsQuery.Where(u => u.BuildingId != null && buildingIds.Contains(u.BuildingId.Value));
        if (floorIds.Count > 0)
            unitsQuery = unitsQuery.Where(u => u.FloorId != null && floorIds.Contains(u.FloorId.Value));

        var unitIds = await unitsQuery.Select(u => u.Id).ToListAsync(cancellationToken);
        if (unitIds.Count == 0)
            return 0;

        var occQuery = _occupancies.Query().AsNoTracking()
            .Where(o => o.IsActive && unitIds.Contains(o.UnitId));
        if (roles.Count > 0)
            occQuery = occQuery.Where(o => roles.Contains(o.Role));

        return await occQuery.Select(o => o.PersonId).Distinct().CountAsync(cancellationToken);
    }

    private async Task EnsurePropertyAsync(Guid horizontalPropertyId, CancellationToken cancellationToken)
    {
        var exists = await _properties.Query().AnyAsync(p => p.Id == horizontalPropertyId, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("Horizontal property was not found.");
    }

    private async Task EnsureUnitAsync(Guid horizontalPropertyId, Guid? unitId, CancellationToken cancellationToken)
    {
        if (!unitId.HasValue)
            return;

        var unit = await _units.Query().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == unitId.Value, cancellationToken)
            ?? throw new InvalidOperationException("Unit was not found.");

        if (unit.HorizontalPropertyId != horizontalPropertyId)
            throw new InvalidOperationException("Unit does not belong to the horizontal property.");
    }

    private async Task EnsureAudienceAsync(
        Guid horizontalPropertyId,
        IReadOnlyList<Guid>? buildingIds,
        IReadOnlyList<Guid>? floorIds,
        CancellationToken cancellationToken)
    {
        var buildings = (buildingIds ?? Array.Empty<Guid>()).Distinct().ToList();
        var floors = (floorIds ?? Array.Empty<Guid>()).Distinct().ToList();

        if (buildings.Count > 0)
        {
            var valid = await _buildings.Query().AsNoTracking()
                .Where(b => b.HorizontalPropertyId == horizontalPropertyId && buildings.Contains(b.Id))
                .Select(b => b.Id)
                .ToListAsync(cancellationToken);
            if (valid.Count != buildings.Count)
                throw new InvalidOperationException("Una o más torres no pertenecen a la copropiedad.");
        }

        if (floors.Count > 0)
        {
            var valid = await (
                from f in _floors.Query().AsNoTracking()
                join b in _buildings.Query().AsNoTracking() on f.BuildingId equals b.Id
                where floors.Contains(f.Id) && b.HorizontalPropertyId == horizontalPropertyId
                select f.Id).ToListAsync(cancellationToken);
            if (valid.Count != floors.Count)
                throw new InvalidOperationException("Uno o más pisos no pertenecen a la copropiedad.");
        }
    }

    private async Task<string> ResolveAuthorDisplayNameAsync(CancellationToken cancellationToken)
    {
        if (_tenant.UserId is not { } userId)
            return "Usuario";

        var user = await _users.Query().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return "Usuario";

        if (!string.IsNullOrWhiteSpace(user.DisplayName))
            return user.DisplayName.Trim();
        return user.Email;
    }

    private static void ValidateExpiry(DateTime? expiresAtUtc)
    {
        if (!expiresAtUtc.HasValue)
            return;

        var expires = expiresAtUtc.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(expiresAtUtc.Value, DateTimeKind.Utc)
            : expiresAtUtc.Value.ToUniversalTime();

        if (expires < DateTime.UtcNow.AddMinutes(-5))
            throw new InvalidOperationException("La fecha de vencimiento no puede estar en el pasado.");
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static MuralPostDto Map(MuralPost entity, IReadOnlyDictionary<Guid, string> unitCodes)
    {
        var now = DateTime.UtcNow;
        return new MuralPostDto
        {
            Id = entity.Id,
            HorizontalPropertyId = entity.HorizontalPropertyId,
            Kind = entity.Kind,
            Category = entity.Category,
            Title = entity.Title,
            Body = entity.Body,
            ContactPhone = entity.ContactPhone,
            ContactEmail = entity.ContactEmail,
            ContactNote = entity.ContactNote,
            AreaLabel = entity.AreaLabel,
            UnitId = entity.UnitId,
            UnitCode = entity.UnitId is { } uid && unitCodes.TryGetValue(uid, out var code) ? code : null,
            AuthorUserId = entity.AuthorUserId,
            AuthorDisplayName = entity.AuthorDisplayName,
            IsPinned = entity.IsPinned,
            IsHidden = entity.IsHidden,
            PublishedAtUtc = entity.PublishedAtUtc,
            ExpiresAtUtc = entity.ExpiresAtUtc,
            IsActive = entity.IsActive,
            IsExpired = entity.ExpiresAtUtc.HasValue && entity.ExpiresAtUtc.Value < now,
            AudienceBuildingIds = MuralAudience.ParseGuids(entity.AudienceBuildingIdsJson),
            AudienceFloorIds = MuralAudience.ParseGuids(entity.AudienceFloorIdsJson),
            AudienceOccupancyRoles = MuralAudience.ParseRoles(entity.AudienceOccupancyRolesJson),
        };
    }
}
