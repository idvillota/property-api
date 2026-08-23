using System.Text.Json;
using Microsoft.Extensions.Options;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Options;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Messaging;

public sealed class OutboxWriter : IOutboxWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenant;
    private readonly OutboxOptions _options;

    public OutboxWriter(
        ApplicationDbContext db,
        ICurrentTenantContext tenant,
        IOptions<OutboxOptions> options)
    {
        _db = db;
        _tenant = tenant;
        _options = options.Value;
    }

    public async Task EnqueueAsync(
        string messageType,
        object payload,
        CancellationToken cancellationToken = default)
    {
        if (_tenant.TenantId is not Guid tenantId || tenantId == Guid.Empty)
            throw new InvalidOperationException("Tenant context is required to enqueue outbox messages.");

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MessageType = messageType,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            Status = OutboxMessageStatus.Pending,
            AttemptCount = 0,
            MaxAttempts = Math.Max(1, _options.DefaultMaxAttempts),
            NextAttemptUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _db.OutboxMessages.AddAsync(message, cancellationToken);
    }
}
