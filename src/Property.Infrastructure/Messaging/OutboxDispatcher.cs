using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Options;
using Property.Domain.Enums;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Messaging;

public sealed class OutboxDispatcher : IOutboxDispatcher
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenant;
    private readonly IEnumerable<IOutboxMessageHandler> _handlers;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(
        ApplicationDbContext db,
        ICurrentTenantContext tenant,
        IEnumerable<IOutboxMessageHandler> handlers,
        IOptions<OutboxOptions> options,
        ILogger<OutboxDispatcher> logger)
    {
        _db = db;
        _tenant = tenant;
        _handlers = handlers;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> DispatchPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var take = Math.Clamp(batchSize, 1, 200);

        // Cross-tenant poll — IgnoreQueryFilters so Pending rows are visible regardless of worker TenantId.
        var due = await _db.OutboxMessages
            .IgnoreQueryFilters()
            .Where(m =>
                m.Status == OutboxMessageStatus.Pending &&
                (m.NextAttemptUtc == null || m.NextAttemptUtc <= now))
            .OrderBy(m => m.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

        foreach (var message in due)
        {
            message.Status = OutboxMessageStatus.Processing;
            message.AttemptCount += 1;
            message.UpdatedAtUtc = now;
        }

        if (due.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);

        foreach (var message in due)
        {
            var previousTenant = _tenant.TenantId;
            try
            {
                // AC2: never process with null tenant (would bypass EF filters).
                _tenant.TenantId = message.TenantId;

                var handler = _handlers.FirstOrDefault(h =>
                    string.Equals(h.MessageType, message.MessageType, StringComparison.Ordinal));
                if (handler is null)
                    throw new InvalidOperationException($"No handler registered for '{message.MessageType}'.");

                await handler.HandleAsync(message.PayloadJson, cancellationToken);

                message.Status = OutboxMessageStatus.Processed;
                message.ProcessedAtUtc = DateTime.UtcNow;
                message.LastError = null;
                message.NextAttemptUtc = null;
                message.UpdatedAtUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Outbox message {MessageId} ({MessageType}) attempt {Attempt} failed",
                    message.Id,
                    message.MessageType,
                    message.AttemptCount);

                message.LastError = Truncate(ex.Message, 2000);
                message.UpdatedAtUtc = DateTime.UtcNow;

                if (message.AttemptCount >= message.MaxAttempts)
                {
                    message.Status = OutboxMessageStatus.DeadLetter;
                    message.NextAttemptUtc = null;
                }
                else
                {
                    message.Status = OutboxMessageStatus.Pending;
                    var delaySeconds = _options.RetryBaseDelaySeconds * (1 << Math.Min(message.AttemptCount - 1, 8));
                    message.NextAttemptUtc = DateTime.UtcNow.AddSeconds(delaySeconds);
                }
            }
            finally
            {
                _tenant.TenantId = previousTenant;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        return due.Count;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
