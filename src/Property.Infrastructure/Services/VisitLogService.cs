using Microsoft.EntityFrameworkCore;
using Property.Application.Auditing;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Common.Options;
using Property.Application.Features.Reception.Visits;
using Property.Application.Messaging;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Common;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class VisitLogService : IVisitLogService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenant;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxWriter _outbox;
    private readonly IAuditWriter _audit;

    public VisitLogService(
        ApplicationDbContext db,
        ICurrentTenantContext tenant,
        IUnitOfWork unitOfWork,
        IOutboxWriter outbox,
        IAuditWriter audit)
    {
        _db = db;
        _tenant = tenant;
        _unitOfWork = unitOfWork;
        _outbox = outbox;
        _audit = audit;
    }

    public async Task<PagedResult<VisitLogDto>> ListAsync(
        ListQuery query,
        CancellationToken cancellationToken = default)
    {
        await ExpireOverdueAsync(cancellationToken);
        return await ListQueryHelpers.ToPagedResultAsync(
            _db.VisitLogs.AsNoTracking()
                .Include(v => v.Unit)
                .Include(v => v.VisitorPerson),
            query,
            q => Shape(q, query),
            entities => Task.FromResult<IReadOnlyList<VisitLogDto>>(entities.Select(Map).ToList()),
            cancellationToken);
    }

    public async Task<VisitLogDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await ExpireOverdueAsync(cancellationToken);
        var entity = await _db.VisitLogs.AsNoTracking()
            .Include(v => v.Unit)
            .Include(v => v.VisitorPerson)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<VisitLogDto> RegisterAsync(
        RegisterVisitDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.UnitId == Guid.Empty)
            throw new InvalidOperationException("Unit is required.");

        var docNumber = NormalizeDocument(dto.DocumentNumber);
        if (string.IsNullOrWhiteSpace(docNumber))
            throw new InvalidOperationException("Document number is required.");

        var firstName = dto.FirstName.Trim();
        if (firstName.Length == 0)
            throw new InvalidOperationException("First name is required.");

        var lastName = string.IsNullOrWhiteSpace(dto.LastName) ? string.Empty : dto.LastName.Trim();

        var propertyExists = await _db.HorizontalProperties
            .AnyAsync(p => p.Id == dto.HorizontalPropertyId, cancellationToken);
        if (!propertyExists)
            throw new InvalidOperationException("Horizontal property was not found.");

        var unit = await _db.Units.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == dto.UnitId, cancellationToken)
            ?? throw new InvalidOperationException("Unit was not found.");
        if (unit.HorizontalPropertyId != dto.HorizontalPropertyId)
            throw new InvalidOperationException("Unit does not belong to the horizontal property.");

        var existing = await _db.People
            .FirstOrDefaultAsync(
                p => p.DocumentType == dto.DocumentType && p.DocumentNumber == docNumber,
                cancellationToken);

        var visitorWasReused = existing is not null;
        Person visitor;
        if (existing is not null)
        {
            visitor = existing;
            visitor.FirstName = firstName;
            visitor.LastName = lastName;
            visitor.IsActive = true;
            _db.People.Update(visitor);
        }
        else
        {
            visitor = new Person
            {
                Id = Guid.NewGuid(),
                DocumentType = dto.DocumentType,
                DocumentNumber = docNumber,
                FirstName = firstName,
                LastName = lastName,
                IsActive = true,
            };
            await _db.People.AddAsync(visitor, cancellationToken);
        }

        var now = DateTime.UtcNow;
        var requiresApproval = dto.RequiresApproval;
        var log = new VisitLog
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = dto.HorizontalPropertyId,
            UnitId = dto.UnitId,
            VisitorPersonId = visitor.Id,
            EnteredAtUtc = now,
            Status = requiresApproval ? VisitLogStatus.PendingApproval : VisitLogStatus.Registered,
            ApprovalExpiresAtUtc = requiresApproval
                ? now.AddMinutes(VisitApprovalOptions.TimeoutMinutes)
                : null,
            RegisteredByUserId = _tenant.UserId,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
        };
        await _db.VisitLogs.AddAsync(log, cancellationToken);

        if (requiresApproval)
            await EnqueueApprovalEmailsAsync(log, unit, visitor, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new VisitLogDto
        {
            Id = log.Id,
            HorizontalPropertyId = log.HorizontalPropertyId,
            UnitId = log.UnitId,
            UnitCode = unit.Code,
            VisitorPersonId = visitor.Id,
            VisitorFullName = $"{visitor.FirstName} {visitor.LastName}".Trim(),
            DocumentType = visitor.DocumentType,
            DocumentNumber = visitor.DocumentNumber,
            EnteredAtUtc = log.EnteredAtUtc,
            Status = log.Status,
            Notes = log.Notes,
            VisitorWasReused = visitorWasReused,
            ApprovalExpiresAtUtc = log.ApprovalExpiresAtUtc,
        };
    }

    public async Task<VisitLogDto> ApproveAsync(
        Guid visitId,
        Guid personId,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        await ExpireOverdueAsync(cancellationToken);
        var visit = await LoadPendingForDecisionAsync(visitId, personId, cancellationToken);
        ApplyDecision(visit, VisitLogStatus.Approved, personId, userId, overrideReason: null);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(visitId, cancellationToken))!;
    }

    public async Task<VisitLogDto> RejectAsync(
        Guid visitId,
        Guid personId,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        await ExpireOverdueAsync(cancellationToken);
        var visit = await LoadPendingForDecisionAsync(visitId, personId, cancellationToken);
        ApplyDecision(visit, VisitLogStatus.Rejected, personId, userId, overrideReason: null);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(visitId, cancellationToken))!;
    }

    public async Task<VisitLogDto> OverrideApproveAsync(
        Guid visitId,
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Override reason is required.");

        await ExpireOverdueAsync(cancellationToken);

        var visit = await _db.VisitLogs
            .FirstOrDefaultAsync(v => v.Id == visitId, cancellationToken)
            ?? throw new InvalidOperationException("Visit was not found.");

        if (visit.Status != VisitLogStatus.PendingApproval)
            throw new InvalidOperationException("Visit is not pending approval.");

        var before = new { visit.Status, visit.OverrideReason };
        ApplyDecision(visit, VisitLogStatus.Approved, personId: null, userId, reason.Trim());

        await _audit.WriteAsync(
            AuditEntityTypes.VisitLog,
            visit.Id.ToString(),
            AuditActions.VisitOverrideApproved,
            before,
            new { visit.Status, visit.OverrideReason, visit.DecidedByUserId, visit.DecidedAtUtc },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(visitId, cancellationToken))!;
    }

    public async Task<int> ExpireOverdueAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var overdue = await _db.VisitLogs
            .Where(v =>
                v.Status == VisitLogStatus.PendingApproval &&
                v.ApprovalExpiresAtUtc != null &&
                v.ApprovalExpiresAtUtc < now)
            .ToListAsync(cancellationToken);

        if (overdue.Count == 0)
            return 0;

        foreach (var visit in overdue)
        {
            visit.Status = VisitLogStatus.Expired;
            visit.DecidedAtUtc = now;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return overdue.Count;
    }

    private async Task<VisitLog> LoadPendingForDecisionAsync(
        Guid visitId,
        Guid personId,
        CancellationToken cancellationToken)
    {
        var visit = await _db.VisitLogs
            .FirstOrDefaultAsync(v => v.Id == visitId, cancellationToken)
            ?? throw new InvalidOperationException("Visit was not found.");

        if (visit.Status != VisitLogStatus.PendingApproval)
            throw new InvalidOperationException("Visit is not pending approval.");

        var hasOccupancy = await _db.UnitOccupancies.AsNoTracking()
            .AnyAsync(
                o => o.PersonId == personId && o.UnitId == visit.UnitId && o.IsActive,
                cancellationToken);
        if (!hasOccupancy)
            throw new UnauthorizedAccessException("Person does not have active occupancy on the visit unit.");

        return visit;
    }

    private static void ApplyDecision(
        VisitLog visit,
        VisitLogStatus status,
        Guid? personId,
        Guid? userId,
        string? overrideReason)
    {
        visit.Status = status;
        visit.DecidedAtUtc = DateTime.UtcNow;
        visit.DecidedByPersonId = personId;
        visit.DecidedByUserId = userId;
        visit.OverrideReason = overrideReason;
    }

    private async Task EnqueueApprovalEmailsAsync(
        VisitLog log,
        Unit unit,
        Person visitor,
        CancellationToken cancellationToken)
    {
        var contacts = await _db.UnitOccupancies.AsNoTracking()
            .Where(o => o.UnitId == log.UnitId && o.IsActive && o.IsCommunicationResponsible)
            .Join(
                _db.People.AsNoTracking(),
                o => o.PersonId,
                p => p.Id,
                (_, p) => p)
            .Where(p => p.Email != null && p.Email != "")
            .ToListAsync(cancellationToken);

        if (contacts.Count == 0)
        {
            contacts = await _db.UnitOccupancies.AsNoTracking()
                .Where(o => o.UnitId == log.UnitId && o.IsActive)
                .Join(
                    _db.People.AsNoTracking(),
                    o => o.PersonId,
                    p => p.Id,
                    (_, p) => p)
                .Where(p => p.Email != null && p.Email != "")
                .ToListAsync(cancellationToken);
        }

        var visitorName = $"{visitor.FirstName} {visitor.LastName}".Trim();
        foreach (var person in contacts.DistinctBy(p => p.Id))
        {
            await _outbox.EnqueueAsync(
                OutboxMessageTypes.EmailSend,
                new EmailSendPayload
                {
                    ToEmail = person.Email!,
                    Subject = $"Visita pendiente de aprobación — unidad {unit.Code}",
                    HtmlBody =
                        $"""
                        <p>Hola {System.Net.WebUtility.HtmlEncode($"{person.FirstName} {person.LastName}".Trim())},</p>
                        <p>Hay una visita pendiente de aprobación para la unidad <strong>{System.Net.WebUtility.HtmlEncode(unit.Code)}</strong>.</p>
                        <p>Visitante: <strong>{System.Net.WebUtility.HtmlEncode(visitorName)}</strong>.</p>
                        <p>Aprueba o rechaza desde el portal en <strong>/portal/visitas</strong>.</p>
                        """,
                },
                cancellationToken);
        }
    }

    private static string NormalizeDocument(string value) =>
        value.Trim().ToUpperInvariant();

    private static IQueryable<VisitLog> Shape(IQueryable<VisitLog> query, ListQuery q)
    {
        if (q.FilterValue("horizontalPropertyId") is { } hpRaw &&
            ListQueryHelpers.TryParseGuid(hpRaw, out var horizontalPropertyId))
            query = query.Where(v => v.HorizontalPropertyId == horizontalPropertyId);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim().ToLowerInvariant();
            query = query.Where(v =>
                (v.VisitorPerson != null && (
                    (v.VisitorPerson.FirstName + " " + v.VisitorPerson.LastName).ToLower().Contains(term) ||
                    v.VisitorPerson.DocumentNumber.ToLower().Contains(term))) ||
                (v.Unit != null && v.Unit.Code.ToLower().Contains(term)));
        }

        var desc = q.IsDescending;
        return (q.SortBy?.ToLowerInvariant() ?? "enteredatutc") switch
        {
            "unitcode" => desc
                ? query.OrderByDescending(v => v.Unit!.Code)
                : query.OrderBy(v => v.Unit!.Code),
            "status" => desc
                ? query.OrderByDescending(v => v.Status)
                : query.OrderBy(v => v.Status),
            _ => desc
                ? query.OrderByDescending(v => v.EnteredAtUtc)
                : query.OrderBy(v => v.EnteredAtUtc),
        };
    }

    private static VisitLogDto Map(VisitLog entity) =>
        new()
        {
            Id = entity.Id,
            HorizontalPropertyId = entity.HorizontalPropertyId,
            UnitId = entity.UnitId,
            UnitCode = entity.Unit?.Code ?? string.Empty,
            VisitorPersonId = entity.VisitorPersonId,
            VisitorFullName = entity.VisitorPerson is null
                ? string.Empty
                : $"{entity.VisitorPerson.FirstName} {entity.VisitorPerson.LastName}".Trim(),
            DocumentType = entity.VisitorPerson?.DocumentType ?? DocumentType.Other,
            DocumentNumber = entity.VisitorPerson?.DocumentNumber ?? string.Empty,
            EnteredAtUtc = entity.EnteredAtUtc,
            Status = entity.Status,
            Notes = entity.Notes,
            VisitorWasReused = false,
            ApprovalExpiresAtUtc = entity.ApprovalExpiresAtUtc,
            DecidedAtUtc = entity.DecidedAtUtc,
            OverrideReason = entity.OverrideReason,
        };
}
