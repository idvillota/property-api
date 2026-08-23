using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Property.Application.Common.Interfaces;
using Property.Application.Messaging;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Common;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class DunningJobService : IDunningJobService
{
    private readonly ApplicationDbContext _db;
    private readonly CurrentTenantContext _tenant;
    private readonly IOutboxWriter _outbox;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DunningJobService> _logger;

    public DunningJobService(
        ApplicationDbContext db,
        ICurrentTenantContext tenant,
        IOutboxWriter outbox,
        IUnitOfWork unitOfWork,
        ILogger<DunningJobService> logger)
    {
        _db = db;
        _tenant = (CurrentTenantContext)tenant;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> RunAsync(DateOnly? asOfDate = null, CancellationToken cancellationToken = default)
    {
        var today = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var rules = await _db.DunningRules.IgnoreQueryFilters()
            .Where(r => r.IsEnabled)
            .OrderBy(r => r.TenantId)
            .ThenBy(r => r.DaysOverdue)
            .ToListAsync(cancellationToken);

        var enqueued = 0;
        foreach (var ruleGroup in rules.GroupBy(r => r.TenantId))
        {
            var previous = _tenant.TenantId;
            _tenant.TenantId = ruleGroup.Key;
            try
            {
                foreach (var rule in ruleGroup)
                    enqueued += await ProcessRuleAsync(rule, today, cancellationToken);
            }
            finally
            {
                _tenant.TenantId = previous;
            }
        }

        if (enqueued > 0)
            _logger.LogInformation("Dunning job enqueued {Count} reminder(s) as of {AsOf}", enqueued, today);

        return enqueued;
    }

    private async Task<int> ProcessRuleAsync(DunningRule rule, DateOnly today, CancellationToken cancellationToken)
    {
        var openCharges = await _db.FeeCharges.AsNoTracking()
            .Where(c =>
                c.HorizontalPropertyId == rule.HorizontalPropertyId &&
                c.IsActive &&
                (c.Status == FeeChargeStatus.Pending || c.Status == FeeChargeStatus.Partial) &&
                c.DueDate < today)
            .Select(c => new { c.UnitId, c.DueDate, Balance = c.Amount - c.AmountPaid })
            .ToListAsync(cancellationToken);

        var unitSummaries = openCharges
            .GroupBy(c => c.UnitId)
            .Select(g =>
            {
                var oldestDue = g.Min(x => x.DueDate);
                var days = today.DayNumber - oldestDue.DayNumber;
                return new
                {
                    UnitId = g.Key,
                    DaysOverdue = days,
                    Balance = Math.Round(g.Sum(x => x.Balance), 2, MidpointRounding.AwayFromZero),
                };
            })
            .Where(u => u.DaysOverdue >= rule.DaysOverdue && u.Balance > 0m)
            .ToList();

        if (unitSummaries.Count == 0)
            return 0;

        var unitIds = unitSummaries.Select(u => u.UnitId).ToList();
        var contacts = await (
            from o in _db.UnitOccupancies.AsNoTracking()
            join p in _db.People.AsNoTracking() on o.PersonId equals p.Id
            where unitIds.Contains(o.UnitId) && o.IsActive && o.IsCommunicationResponsible
            select new
            {
                o.UnitId,
                PersonId = p.Id,
                p.Email,
                p.Phone,
                p.WhatsAppOptIn,
                Name = (p.FirstName + " " + p.LastName).Trim(),
            }
        ).ToListAsync(cancellationToken);

        var contactByUnit = contacts
            .GroupBy(c => c.UnitId)
            .ToDictionary(g => g.Key, g => g.First());

        var unitCodes = await _db.Units.AsNoTracking()
            .Where(u => unitIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Code })
            .ToDictionaryAsync(u => u.Id, u => u.Code, cancellationToken);

        var cooldownFrom = DateTime.UtcNow.AddDays(-rule.CooldownDays);
        var recentUnitIds = await _db.DunningSendLogs.AsNoTracking()
            .Where(l =>
                l.DunningRuleId == rule.Id &&
                unitIds.Contains(l.UnitId) &&
                l.SentAtUtc >= cooldownFrom)
            .Select(l => l.UnitId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var cooled = recentUnitIds.ToHashSet();

        var count = 0;
        foreach (var unit in unitSummaries)
        {
            if (cooled.Contains(unit.UnitId))
                continue;
            if (!contactByUnit.TryGetValue(unit.UnitId, out var contact))
                continue;

            var hasEmail = !string.IsNullOrWhiteSpace(contact.Email);
            var hasWhatsApp = contact.WhatsAppOptIn && !string.IsNullOrWhiteSpace(contact.Phone);
            if (!hasEmail && !hasWhatsApp)
                continue;

            unitCodes.TryGetValue(unit.UnitId, out var code);
            code ??= unit.UnitId.ToString("N")[..8];

            var subject = ApplyTemplate(
                rule.EmailSubjectTemplate,
                code,
                contact.Name,
                unit.Balance,
                unit.DaysOverdue);
            var body = ApplyTemplate(
                rule.EmailBodyTemplate,
                code,
                contact.Name,
                unit.Balance,
                unit.DaysOverdue);

            Guid? emailOutboxId = null;
            if (hasEmail)
            {
                await _outbox.EnqueueAsync(
                    OutboxMessageTypes.EmailSend,
                    new EmailSendPayload
                    {
                        ToEmail = contact.Email!.Trim(),
                        Subject = subject,
                        HtmlBody = body,
                    },
                    cancellationToken);
                emailOutboxId = GetLatestPendingOutboxId();
            }

            Guid? whatsAppOutboxId = null;
            if (hasWhatsApp)
            {
                await _outbox.EnqueueAsync(
                    OutboxMessageTypes.WhatsAppSend,
                    new WhatsAppSendPayload
                    {
                        ToPhone = contact.Phone!.Trim(),
                        Body = StripHtml(body),
                        TemplateKey = "dunning",
                    },
                    cancellationToken);
                whatsAppOutboxId = GetLatestPendingOutboxId();

                await _db.NotificationLogs.AddAsync(
                    new NotificationLog
                    {
                        Id = Guid.NewGuid(),
                        TenantId = rule.TenantId,
                        HorizontalPropertyId = rule.HorizontalPropertyId,
                        PersonId = contact.PersonId,
                        UnitId = unit.UnitId,
                        Channel = "whatsapp",
                        Purpose = "dunning",
                        ToAddress = contact.Phone!.Trim(),
                        TemplateKey = "dunning",
                        OutboxMessageId = whatsAppOutboxId,
                        SentAtUtc = DateTime.UtcNow,
                    },
                    cancellationToken);
            }

            if (!hasEmail)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                count++;
                continue;
            }

            var log = new DunningSendLog
            {
                Id = Guid.NewGuid(),
                TenantId = rule.TenantId,
                HorizontalPropertyId = rule.HorizontalPropertyId,
                UnitId = unit.UnitId,
                DunningRuleId = rule.Id,
                PersonId = contact.PersonId,
                ToEmail = contact.Email!.Trim(),
                DaysOverdueSnapshot = unit.DaysOverdue,
                BalanceSnapshot = unit.Balance,
                SentAtUtc = DateTime.UtcNow,
                OutboxMessageId = emailOutboxId,
            };
            await _db.DunningSendLogs.AddAsync(log, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            count++;
        }

        return count;
    }

    private Guid? GetLatestPendingOutboxId() =>
        _db.ChangeTracker.Entries<OutboxMessage>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .OrderByDescending(e => e.CreatedAtUtc)
            .Select(e => (Guid?)e.Id)
            .FirstOrDefault();

    private static string StripHtml(string html) =>
        System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ").Trim();

    private static string ApplyTemplate(
        string template,
        string unitCode,
        string personName,
        decimal balance,
        int daysOverdue) =>
        template
            .Replace("{{unitCode}}", unitCode, StringComparison.OrdinalIgnoreCase)
            .Replace("{{personName}}", personName, StringComparison.OrdinalIgnoreCase)
            .Replace("{{balance}}", balance.ToString("0.00"), StringComparison.OrdinalIgnoreCase)
            .Replace("{{daysOverdue}}", daysOverdue.ToString(), StringComparison.OrdinalIgnoreCase);
}
