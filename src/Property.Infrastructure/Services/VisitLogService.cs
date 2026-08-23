using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Reception.Visits;
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

    public VisitLogService(
        ApplicationDbContext db,
        ICurrentTenantContext tenant,
        IUnitOfWork unitOfWork)
    {
        _db = db;
        _tenant = tenant;
        _unitOfWork = unitOfWork;
    }

    public Task<PagedResult<VisitLogDto>> ListAsync(
        ListQuery query,
        CancellationToken cancellationToken = default) =>
        ListQueryHelpers.ToPagedResultAsync(
            _db.VisitLogs.AsNoTracking()
                .Include(v => v.Unit)
                .Include(v => v.VisitorPerson),
            query,
            q => Shape(q, query),
            entities => Task.FromResult<IReadOnlyList<VisitLogDto>>(entities.Select(Map).ToList()),
            cancellationToken);

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
            // Keep name fresh when reusing (portería may correct spelling)
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

        var log = new VisitLog
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = dto.HorizontalPropertyId,
            UnitId = dto.UnitId,
            VisitorPersonId = visitor.Id,
            EnteredAtUtc = DateTime.UtcNow,
            Status = VisitLogStatus.Registered,
            RegisteredByUserId = _tenant.UserId,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
        };
        await _db.VisitLogs.AddAsync(log, cancellationToken);
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
        };
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
        };
}
