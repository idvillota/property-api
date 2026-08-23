using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Reception.Visits;
using Property.Application.Messaging;
using Property.Domain.Entities;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class IntercomService : IIntercomService
{
    public const string NotificationType = "visit.intercom";

    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenant;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxWriter _outbox;
    private readonly IVisitIntercomRealtime _realtime;

    public IntercomService(
        ApplicationDbContext db,
        ICurrentTenantContext tenant,
        IUnitOfWork unitOfWork,
        IOutboxWriter outbox,
        IVisitIntercomRealtime realtime)
    {
        _db = db;
        _tenant = tenant;
        _unitOfWork = unitOfWork;
        _outbox = outbox;
        _realtime = realtime;
    }

    public async Task NotifyUnitAsync(Guid visitId, CancellationToken cancellationToken = default)
    {
        if (_tenant.TenantId is not Guid tenantId || tenantId == Guid.Empty)
            throw new InvalidOperationException("Tenant context is required.");

        var visit = await _db.VisitLogs
            .Include(v => v.Unit)
            .Include(v => v.VisitorPerson)
            .FirstOrDefaultAsync(v => v.Id == visitId, cancellationToken)
            ?? throw new InvalidOperationException("Visit was not found.");

        var unitCode = visit.Unit?.Code ?? string.Empty;
        var visitorName = visit.VisitorPerson is null
            ? string.Empty
            : $"{visit.VisitorPerson.FirstName} {visit.VisitorPerson.LastName}".Trim();

        var title = "Visita en portería";
        var body = $"Hay un visitante ({visitorName}) esperando autorización para la unidad {unitCode}.";
        var linkPath = "/portal/visitas";

        var recipients = await ResolveRecipientsAsync(visit.UnitId, cancellationToken);
        if (recipients.Count == 0)
            throw new InvalidOperationException("No occupants found for the visit unit.");

        var alert = new VisitIntercomAlertDto
        {
            VisitId = visit.Id,
            UnitId = visit.UnitId,
            UnitCode = unitCode,
            VisitorFullName = visitorName,
            Title = title,
            Body = body,
            LinkPath = linkPath,
        };

        foreach (var person in recipients)
        {
            Guid? userId = await _db.UserPersonLinks.AsNoTracking()
                .Where(l => l.PersonId == person.Id && l.IsActive)
                .Select(l => (Guid?)l.UserId)
                .FirstOrDefaultAsync(cancellationToken);

            await _db.InAppNotifications.AddAsync(
                new InAppNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    PersonId = person.Id,
                    Type = NotificationType,
                    Title = title,
                    Body = body,
                    LinkPath = linkPath,
                    RelatedEntityId = visit.Id,
                    IsRead = false,
                },
                cancellationToken);

            await _realtime.BroadcastToPersonAsync(tenantId, person.Id, alert, cancellationToken);

            // MVP: always enqueue email fallback (covers offline residents).
            if (!string.IsNullOrWhiteSpace(person.Email))
            {
                await _outbox.EnqueueAsync(
                    OutboxMessageTypes.EmailSend,
                    new EmailSendPayload
                    {
                        ToEmail = person.Email!,
                        Subject = $"{title} — unidad {unitCode}",
                        HtmlBody =
                            $"""
                            <p>Hola {System.Net.WebUtility.HtmlEncode($"{person.FirstName} {person.LastName}".Trim())},</p>
                            <p>{System.Net.WebUtility.HtmlEncode(body)}</p>
                            <p>Abre el portal: <strong>{System.Net.WebUtility.HtmlEncode(linkPath)}</strong></p>
                            """,
                    },
                    cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<Person>> ResolveRecipientsAsync(Guid unitId, CancellationToken cancellationToken)
    {
        var responsible = await _db.UnitOccupancies.AsNoTracking()
            .Where(o => o.UnitId == unitId && o.IsActive && o.IsCommunicationResponsible)
            .Join(
                _db.People.AsNoTracking().Where(p => p.IsActive),
                o => o.PersonId,
                p => p.Id,
                (_, p) => p)
            .ToListAsync(cancellationToken);

        if (responsible.Count > 0)
            return responsible.DistinctBy(p => p.Id).ToList();

        return await _db.UnitOccupancies.AsNoTracking()
            .Where(o => o.UnitId == unitId && o.IsActive)
            .Join(
                _db.People.AsNoTracking().Where(p => p.IsActive),
                o => o.PersonId,
                p => p.Id,
                (_, p) => p)
            .ToListAsync(cancellationToken);
    }
}
