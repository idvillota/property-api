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
    private readonly IRepository<HorizontalProperty> _properties;
    private readonly IRepository<Unit> _units;
    private readonly IRepository<User> _users;
    private readonly ICurrentTenantContext _tenant;
    private readonly IUnitOfWork _unitOfWork;

    public MuralPostService(
        IRepository<MuralPost> posts,
        IRepository<HorizontalProperty> properties,
        IRepository<Unit> units,
        IRepository<User> users,
        ICurrentTenantContext tenant,
        IUnitOfWork unitOfWork)
    {
        _posts = posts;
        _properties = properties;
        _units = units;
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
                IReadOnlyList<MuralPostDto> items = entities.Select(e => Map(e, unitCodes)).ToList();
                return items;
            },
            cancellationToken);

    public async Task<IReadOnlyList<MuralPostDto>> ListPublicAsync(
        Guid? horizontalPropertyId = null,
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
            .Take(100)
            .ToListAsync(cancellationToken);

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
        return Map(entity, codes);
    }

    public async Task<MuralPostDto> CreateAsync(CreateMuralPostDto dto, CancellationToken cancellationToken = default)
    {
        await EnsurePropertyAsync(dto.HorizontalPropertyId, cancellationToken);
        await EnsureUnitAsync(dto.HorizontalPropertyId, dto.UnitId, cancellationToken);
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
        };
    }
}
