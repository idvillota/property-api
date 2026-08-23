using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Community.Pqrs;
using Property.Application.Messaging;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Common;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class PqrsService : IPqrsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenant;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxWriter _outbox;
    private readonly IWebhookPublisher _webhooks;

    public PqrsService(
        ApplicationDbContext db,
        ICurrentTenantContext tenant,
        IUnitOfWork unitOfWork,
        IOutboxWriter outbox,
        IWebhookPublisher webhooks)
    {
        _db = db;
        _tenant = tenant;
        _unitOfWork = unitOfWork;
        _outbox = outbox;
        _webhooks = webhooks;
    }

    public Task<PagedResult<PqrsCaseDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default) =>
        ListQueryHelpers.ToPagedResultAsync(
            _db.PqrsCases.AsNoTracking(),
            query,
            q => PagedEntityQueries.ShapePqrsCases(q, query),
            async entities =>
            {
                var unitIds = entities.Select(e => e.UnitId).Distinct().ToList();
                var personIds = entities.Select(e => e.CreatedByPersonId).Distinct().ToList();

                var unitCodes = unitIds.Count == 0
                    ? new Dictionary<Guid, string>()
                    : await _db.Units.AsNoTracking()
                        .Where(u => unitIds.Contains(u.Id))
                        .ToDictionaryAsync(u => u.Id, u => u.Code, cancellationToken);

                var personNames = personIds.Count == 0
                    ? new Dictionary<Guid, string>()
                    : await _db.People.AsNoTracking()
                        .Where(p => personIds.Contains(p.Id))
                        .ToDictionaryAsync(
                            p => p.Id,
                            p => $"{p.FirstName} {p.LastName}".Trim(),
                            cancellationToken);

                IReadOnlyList<PqrsCaseDto> items = entities
                    .Select(e => MapCase(e, unitCodes, personNames, includeComments: false))
                    .ToList();
                return items;
            },
            cancellationToken);

    public async Task<PqrsCaseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.PqrsCases.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id && e.IsActive, cancellationToken);
        if (entity is null)
            return null;

        var comments = await _db.PqrsComments.AsNoTracking()
            .Where(c => c.PqrsCaseId == id)
            .OrderBy(c => c.PostedAtUtc)
            .ToListAsync(cancellationToken);

        return await MapCaseWithLookupsAsync(entity, comments, cancellationToken);
    }

    public async Task<PqrsCaseDto> CreateAsync(CreatePqrsCaseDto dto, CancellationToken cancellationToken = default)
    {
        var propertyExists = await _db.HorizontalProperties.AsNoTracking()
            .AnyAsync(p => p.Id == dto.HorizontalPropertyId, cancellationToken);
        if (!propertyExists)
            throw new InvalidOperationException("Horizontal property was not found.");

        var unit = await _db.Units.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == dto.UnitId && u.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Unit was not found.");

        if (unit.HorizontalPropertyId != dto.HorizontalPropertyId)
            throw new InvalidOperationException("Unit does not belong to the horizontal property.");

        var personExists = await _db.People.AsNoTracking()
            .AnyAsync(p => p.Id == dto.CreatedByPersonId && p.IsActive, cancellationToken);
        if (!personExists)
            throw new InvalidOperationException("Person was not found.");

        var entity = new PqrsCase
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = dto.HorizontalPropertyId,
            UnitId = dto.UnitId,
            CreatedByPersonId = dto.CreatedByPersonId,
            CreatedByUserId = _tenant.UserId,
            Category = dto.Category,
            Subject = dto.Subject.Trim(),
            Description = dto.Description.Trim(),
            Status = PqrsStatus.Open,
            DueAtUtc = dto.DueAtUtc?.ToUniversalTime(),
            AssignedToUserId = dto.AssignedToUserId,
            AttachmentFileAssetIdsJson = SerializeGuids(dto.AttachmentFileAssetIds),
            IsActive = true,
        };

        await _db.PqrsCases.AddAsync(entity, cancellationToken);
        await _webhooks.EnqueueEventAsync(
            WebhookEventTypes.PqrsCreated,
            new
            {
                pqrsId = entity.Id,
                horizontalPropertyId = entity.HorizontalPropertyId,
                unitId = entity.UnitId,
                createdByPersonId = entity.CreatedByPersonId,
                category = entity.Category.ToString(),
                subject = entity.Subject,
                status = entity.Status.ToString(),
            },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<PqrsCaseDto?> TransitionStatusAsync(
        Guid id,
        PqrsStatus status,
        DateTime? dueAtUtc = null,
        string? comment = null,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.PqrsCases.FirstOrDefaultAsync(e => e.Id == id && e.IsActive, cancellationToken);
        if (entity is null)
            return null;

        var previous = entity.Status;
        entity.Status = status;
        if (dueAtUtc.HasValue)
            entity.DueAtUtc = dueAtUtc.Value.ToUniversalTime();

        if (!string.IsNullOrWhiteSpace(comment))
        {
            var authorName = await ResolveAuthorDisplayNameAsync(cancellationToken);
            await _db.PqrsComments.AddAsync(
                new PqrsComment
                {
                    Id = Guid.NewGuid(),
                    PqrsCaseId = entity.Id,
                    AuthorUserId = _tenant.UserId,
                    AuthorDisplayName = authorName,
                    Body = comment.Trim(),
                    IsInternal = false,
                    PostedAtUtc = DateTime.UtcNow,
                },
                cancellationToken);
        }

        if (previous != status)
            await EnqueueStatusChangeEmailAsync(entity, previous, status, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<PqrsCommentDto?> AddCommentAsync(
        Guid caseId,
        AddPqrsCommentDto dto,
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.PqrsCases.AsNoTracking()
            .AnyAsync(e => e.Id == caseId && e.IsActive, cancellationToken);
        if (!exists)
            return null;

        var authorName = await ResolveAuthorDisplayNameAsync(cancellationToken);
        var comment = new PqrsComment
        {
            Id = Guid.NewGuid(),
            PqrsCaseId = caseId,
            AuthorUserId = _tenant.UserId,
            AuthorDisplayName = authorName,
            Body = dto.Body.Trim(),
            IsInternal = dto.IsInternal,
            PostedAtUtc = DateTime.UtcNow,
        };

        await _db.PqrsComments.AddAsync(comment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapComment(comment);
    }

    public async Task<IReadOnlyList<PqrsCaseDto>> ListMineAsync(
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _db.PqrsCases.AsNoTracking()
            .Where(e => e.IsActive && e.CreatedByPersonId == personId)
            .OrderByDescending(e => e.CreatedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (entities.Count == 0)
            return Array.Empty<PqrsCaseDto>();

        var unitIds = entities.Select(e => e.UnitId).Distinct().ToList();
        var unitCodes = await _db.Units.AsNoTracking()
            .Where(u => unitIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Code, cancellationToken);

        var personName = await _db.People.AsNoTracking()
            .Where(p => p.Id == personId)
            .Select(p => (p.FirstName + " " + p.LastName).Trim())
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var personNames = new Dictionary<Guid, string> { [personId] = personName };

        return entities.Select(e => MapCase(e, unitCodes, personNames, includeComments: false)).ToList();
    }

    public async Task<PqrsCaseDto?> GetMineByIdAsync(
        Guid personId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.PqrsCases.AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.Id == id && e.IsActive && e.CreatedByPersonId == personId,
                cancellationToken);
        if (entity is null)
            return null;

        var comments = await _db.PqrsComments.AsNoTracking()
            .Where(c => c.PqrsCaseId == id && !c.IsInternal)
            .OrderBy(c => c.PostedAtUtc)
            .ToListAsync(cancellationToken);

        return await MapCaseWithLookupsAsync(entity, comments, cancellationToken);
    }

    private async Task EnqueueStatusChangeEmailAsync(
        PqrsCase entity,
        PqrsStatus previous,
        PqrsStatus next,
        CancellationToken cancellationToken)
    {
        var person = await _db.People.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == entity.CreatedByPersonId, cancellationToken);
        if (person is null || string.IsNullOrWhiteSpace(person.Email))
            return;

        var statusLabel = StatusLabelEs(next);
        var previousLabel = StatusLabelEs(previous);
        var subject = $"Actualización de tu PQRS: {entity.Subject}";
        var body =
            $"<p>Hola {Escape(person.FirstName)},</p>" +
            $"<p>Tu caso PQRS <strong>{Escape(entity.Subject)}</strong> cambió de estado " +
            $"de <strong>{previousLabel}</strong> a <strong>{statusLabel}</strong>.</p>" +
            "<p>Puedes consultar el detalle en el portal de residentes.</p>";

        await _outbox.EnqueueAsync(
            OutboxMessageTypes.EmailSend,
            new EmailSendPayload
            {
                ToEmail = person.Email.Trim(),
                Subject = subject,
                HtmlBody = body,
            },
            cancellationToken);
    }

    private async Task<string> ResolveAuthorDisplayNameAsync(CancellationToken cancellationToken)
    {
        if (_tenant.UserId is not { } userId)
            return "Administración";

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return "Administración";

        if (!string.IsNullOrWhiteSpace(user.DisplayName))
            return user.DisplayName.Trim();
        return user.Email;
    }

    private async Task<PqrsCaseDto> MapCaseWithLookupsAsync(
        PqrsCase entity,
        IReadOnlyList<PqrsComment> comments,
        CancellationToken cancellationToken)
    {
        var unitCode = await _db.Units.AsNoTracking()
            .Where(u => u.Id == entity.UnitId)
            .Select(u => u.Code)
            .FirstOrDefaultAsync(cancellationToken);

        var personName = await _db.People.AsNoTracking()
            .Where(p => p.Id == entity.CreatedByPersonId)
            .Select(p => (p.FirstName + " " + p.LastName).Trim())
            .FirstOrDefaultAsync(cancellationToken);

        var unitCodes = unitCode is null
            ? new Dictionary<Guid, string>()
            : new Dictionary<Guid, string> { [entity.UnitId] = unitCode };
        var personNames = personName is null
            ? new Dictionary<Guid, string>()
            : new Dictionary<Guid, string> { [entity.CreatedByPersonId] = personName };

        var dto = MapCase(entity, unitCodes, personNames, includeComments: true);
        dto.Comments = comments.Select(MapComment).ToList();
        return dto;
    }

    private static PqrsCaseDto MapCase(
        PqrsCase entity,
        IReadOnlyDictionary<Guid, string> unitCodes,
        IReadOnlyDictionary<Guid, string> personNames,
        bool includeComments) =>
        new()
        {
            Id = entity.Id,
            HorizontalPropertyId = entity.HorizontalPropertyId,
            UnitId = entity.UnitId,
            UnitCode = unitCodes.TryGetValue(entity.UnitId, out var code) ? code : null,
            CreatedByPersonId = entity.CreatedByPersonId,
            CreatedByPersonName = personNames.TryGetValue(entity.CreatedByPersonId, out var name) ? name : null,
            CreatedByUserId = entity.CreatedByUserId,
            Category = entity.Category,
            Subject = entity.Subject,
            Description = entity.Description,
            Status = entity.Status,
            DueAtUtc = entity.DueAtUtc,
            AssignedToUserId = entity.AssignedToUserId,
            AttachmentFileAssetIds = ParseGuids(entity.AttachmentFileAssetIdsJson),
            IsActive = entity.IsActive,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
            Comments = includeComments ? Array.Empty<PqrsCommentDto>() : Array.Empty<PqrsCommentDto>(),
        };

    private static PqrsCommentDto MapComment(PqrsComment c) =>
        new()
        {
            Id = c.Id,
            PqrsCaseId = c.PqrsCaseId,
            AuthorUserId = c.AuthorUserId,
            AuthorPersonId = c.AuthorPersonId,
            AuthorDisplayName = c.AuthorDisplayName,
            Body = c.Body,
            IsInternal = c.IsInternal,
            PostedAtUtc = c.PostedAtUtc,
        };

    private static string SerializeGuids(IEnumerable<Guid>? ids)
    {
        var list = (ids ?? Enumerable.Empty<Guid>()).Distinct().ToList();
        return JsonSerializer.Serialize(list, JsonOptions);
    }

    private static IReadOnlyList<Guid> ParseGuids(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<Guid>();

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json, JsonOptions)?.Distinct().ToList()
                   ?? (IReadOnlyList<Guid>)Array.Empty<Guid>();
        }
        catch (JsonException)
        {
            return Array.Empty<Guid>();
        }
    }

    private static string StatusLabelEs(PqrsStatus status) =>
        status switch
        {
            PqrsStatus.Open => "Abierto",
            PqrsStatus.InProgress => "En progreso",
            PqrsStatus.Resolved => "Resuelto",
            PqrsStatus.Closed => "Cerrado",
            _ => status.ToString(),
        };

    private static string Escape(string? value) =>
        System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
}
