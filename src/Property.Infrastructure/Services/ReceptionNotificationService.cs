using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Property.Application.Common.Interfaces;
using Property.Application.Messaging;
using Property.Domain.Entities;
using Property.Domain.Enums;

namespace Property.Infrastructure.Services;

public sealed class ReceptionNotificationService : IReceptionNotificationService
{
    private readonly IRepository<ReceptionItem> _items;
    private readonly IRepository<Person> _people;
    private readonly IRepository<Unit> _units;
    private readonly IRepository<UnitOccupancy> _occupancies;
    private readonly IRepository<HorizontalProperty> _properties;
    private readonly IOutboxWriter _outbox;
    private readonly ILogger<ReceptionNotificationService> _logger;

    public ReceptionNotificationService(
        IRepository<ReceptionItem> items,
        IRepository<Person> people,
        IRepository<Unit> units,
        IRepository<UnitOccupancy> occupancies,
        IRepository<HorizontalProperty> properties,
        IOutboxWriter outbox,
        ILogger<ReceptionNotificationService> logger)
    {
        _items = items;
        _people = people;
        _units = units;
        _occupancies = occupancies;
        _properties = properties;
        _outbox = outbox;
        _logger = logger;
    }

    public Task NotifyItemStartedAsync(Guid receptionItemId, CancellationToken cancellationToken = default) =>
        NotifyAsync(receptionItemId, completed: false, cancellationToken);

    public Task NotifyItemCompletedAsync(Guid receptionItemId, CancellationToken cancellationToken = default) =>
        NotifyAsync(receptionItemId, completed: true, cancellationToken);

    private async Task NotifyAsync(Guid receptionItemId, bool completed, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _items.Query()
                .FirstOrDefaultAsync(e => e.Id == receptionItemId, cancellationToken);
            if (item is null)
                return;

            var recipients = await ResolveRecipientsAsync(item, cancellationToken);
            if (recipients.Count == 0)
            {
                _logger.LogInformation(
                    "Skipping reception email for item {ItemId}: no communication contact with email",
                    receptionItemId);
                return;
            }

            var unitCode = item.UnitId.HasValue
                ? await _units.Query().AsNoTracking()
                    .Where(u => u.Id == item.UnitId.Value)
                    .Select(u => u.Code)
                    .FirstOrDefaultAsync(cancellationToken)
                : null;

            var propertyName = await _properties.Query().AsNoTracking()
                .Where(p => p.Id == item.HorizontalPropertyId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(cancellationToken);

            string? deliveredToHabitantName = null;
            if (completed &&
                item.Direction == ReceptionDirection.Inbound &&
                item.DeliveredToPersonId is { } deliveredToId)
            {
                deliveredToHabitantName = await _people.Query().AsNoTracking()
                    .Where(p => p.Id == deliveredToId)
                    .Select(p => ((p.FirstName ?? "") + " " + (p.LastName ?? "")).Trim())
                    .FirstOrDefaultAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(deliveredToHabitantName))
                    deliveredToHabitantName = null;
            }

            foreach (var person in recipients)
            {
                var (subject, body) = BuildMessage(
                    item,
                    person,
                    unitCode,
                    propertyName,
                    deliveredToHabitantName,
                    completed);

                await _outbox.EnqueueAsync(
                    OutboxMessageTypes.EmailSend,
                    new EmailSendPayload
                    {
                        ToEmail = person.Email!,
                        Subject = subject,
                        HtmlBody = body,
                    },
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enqueue reception notification for item {ItemId}", receptionItemId);
        }
    }

    private async Task<List<Person>> ResolveRecipientsAsync(ReceptionItem item, CancellationToken cancellationToken)
    {
        var recipients = new List<Person>();
        if (item.UnitId is { } unitId)
        {
            var responsibleIds = await _occupancies.Query().AsNoTracking()
                .Where(o => o.UnitId == unitId && o.IsActive && o.IsCommunicationResponsible)
                .Select(o => o.PersonId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (responsibleIds.Count > 0)
            {
                recipients = await _people.Query().AsNoTracking()
                    .Where(p => responsibleIds.Contains(p.Id) && p.Email != null && p.Email != "")
                    .ToListAsync(cancellationToken);
            }
        }

        if (recipients.Count == 0 && item.PersonId is { } personId)
        {
            var person = await _people.Query().AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);
            if (person is not null && !string.IsNullOrWhiteSpace(person.Email))
                recipients.Add(person);
        }

        return recipients;
    }

    private static (string Subject, string Body) BuildMessage(
        ReceptionItem item,
        Person person,
        string? unitCode,
        string? propertyName,
        string? deliveredToHabitantName,
        bool completed)
    {
        var personName = $"{person.FirstName} {person.LastName}".Trim();
        var unitLabel = string.IsNullOrWhiteSpace(unitCode) ? "tu unidad" : $"la unidad {unitCode}";
        var typeLabel = ItemTypeLabel(item.ItemType);
        var outbound = item.Direction == ReceptionDirection.Outbound;
        var brand = SubjectBrand(propertyName);
        var notesHtml = NotesBlock(item.Notes);

        if (!completed)
        {
            if (IsUtilityBill(item.ItemType))
            {
                return (
                    $"{brand} - Factura de {typeLabel} disponible",
                    $"""
                    <p>Hola {Web(personName)},</p>
                    <p>Hay una <strong>factura de {Web(typeLabel)}</strong> pendiente de retiro en portería para {Web(unitLabel)}.</p>
                    <p>Por favor acércate a portería para recogerla.</p>
                    {notesHtml}
                    """);
            }

            if (outbound)
            {
                return (
                    $"{brand} - Encargo registrado para retiro",
                    $"""
                    <p>Hola {Web(personName)},</p>
                    <p>Registramos tu encargo (<strong>{Web(typeLabel)}</strong>) en portería para {Web(unitLabel)}. Está pendiente de retiro externo.</p>
                    {notesHtml}
                    """);
            }

            return (
                $"{brand} - {typeLabel} pendiente de retiro",
                $"""
                <p>Hola {Web(personName)},</p>
                <p>Hay un ítem entrante (<strong>{Web(typeLabel)}</strong>) pendiente de retiro en portería para {Web(unitLabel)}.</p>
                <p>Por favor acércate a portería para recogerlo.</p>
                {notesHtml}
                """);
        }

        if (IsUtilityBill(item.ItemType))
        {
            var toClause = InboundDeliveredToClause(deliveredToHabitantName);
            return (
                $"{brand} - Factura de {typeLabel} entregada",
                $"""
                <p>Hola {Web(personName)},</p>
                <p>Confirmamos la entrega de la <strong>factura de {Web(typeLabel)}</strong> de {Web(unitLabel)}{toClause}.</p>
                {notesHtml}
                """);
        }

        if (outbound)
        {
            var to = string.IsNullOrWhiteSpace(item.DeliveredToExternalName)
                ? "el receptor autorizado"
                : item.DeliveredToExternalName;
            return (
                $"{brand} - Encargo entregado",
                $"""
                <p>Hola {Web(personName)},</p>
                <p>Tu encargo (<strong>{Web(typeLabel)}</strong>) de {Web(unitLabel)} fue entregado a <strong>{Web(to)}</strong>.</p>
                {notesHtml}
                """);
        }

        {
            var toClause = InboundDeliveredToClause(deliveredToHabitantName);
            return (
                $"{brand} - {typeLabel} entregado",
                $"""
                <p>Hola {Web(personName)},</p>
                <p>Confirmamos la entrega del ítem entrante (<strong>{Web(typeLabel)}</strong>) de {Web(unitLabel)}{toClause}.</p>
                {notesHtml}
                """);
        }
    }

    private static string InboundDeliveredToClause(string? deliveredToHabitantName) =>
        string.IsNullOrWhiteSpace(deliveredToHabitantName)
            ? string.Empty
            : $" a <strong>{Web(deliveredToHabitantName)}</strong>";

    private static string NotesBlock(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return string.Empty;

        return $"""
            <p><strong>Nota de portería:</strong> {Web(notes.Trim())}</p>
            """;
    }

    private static string SubjectBrand(string? propertyName)
    {
        var name = string.IsNullOrWhiteSpace(propertyName) ? "Portería" : propertyName.Trim();
        return name.EndsWith("Portería", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("Porteria", StringComparison.OrdinalIgnoreCase)
            ? name
            : $"{name} Portería";
    }

    private static bool IsUtilityBill(ReceptionItemType type) =>
        type is ReceptionItemType.WaterBill
            or ReceptionItemType.EnergyBill
            or ReceptionItemType.GasBill
            or ReceptionItemType.AdministrationBill;

    private static string ItemTypeLabel(ReceptionItemType type) => type switch
    {
        ReceptionItemType.Package => "Paquete",
        ReceptionItemType.Letter => "Carta",
        ReceptionItemType.Document => "Documento",
        ReceptionItemType.VisitorMessage => "Mensaje de visitante",
        ReceptionItemType.WaterBill => "agua",
        ReceptionItemType.EnergyBill => "energía",
        ReceptionItemType.GasBill => "gas",
        ReceptionItemType.AdministrationBill => "administración",
        _ => "Otro",
    };

    private static string Web(string value) =>
        System.Net.WebUtility.HtmlEncode(value);
}
