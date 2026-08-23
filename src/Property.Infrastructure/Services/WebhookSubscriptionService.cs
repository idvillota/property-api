using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Webhooks;
using Property.Application.Messaging;
using Property.Domain.Entities;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class WebhookSubscriptionService : IWebhookSubscriptionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ApplicationDbContext _db;
    private readonly IUnitOfWork _unitOfWork;

    public WebhookSubscriptionService(ApplicationDbContext db, IUnitOfWork unitOfWork)
    {
        _db = db;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<WebhookSubscriptionDto>> ListAsync(
        CancellationToken cancellationToken = default) =>
        await _db.WebhookSubscriptions.AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => Map(s))
            .ToListAsync(cancellationToken);

    public async Task<WebhookSubscriptionDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.WebhookSubscriptions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<WebhookSubscriptionDto> CreateAsync(
        CreateWebhookSubscriptionDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidateDto(dto.Name, dto.TargetUrl, dto.Secret, dto.EventTypes);

        var entity = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            TargetUrl = dto.TargetUrl.Trim(),
            Secret = dto.Secret.Trim(),
            EventTypes = SerializeEventTypes(dto.EventTypes),
            IsActive = dto.IsActive,
        };

        await _db.WebhookSubscriptions.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<WebhookSubscriptionDto?> UpdateAsync(
        Guid id,
        UpdateWebhookSubscriptionDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidateDto(dto.Name, dto.TargetUrl, dto.Secret ?? "placeholder", dto.EventTypes);

        var entity = await _db.WebhookSubscriptions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity is null)
            return null;

        entity.Name = dto.Name.Trim();
        entity.TargetUrl = dto.TargetUrl.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Secret))
            entity.Secret = dto.Secret.Trim();
        entity.EventTypes = SerializeEventTypes(dto.EventTypes);
        entity.IsActive = dto.IsActive;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.WebhookSubscriptions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity is null)
            return false;

        _db.WebhookSubscriptions.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ValidateDto(
        string name,
        string targetUrl,
        string secret,
        IReadOnlyList<string> eventTypes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Name is required.");
        if (string.IsNullOrWhiteSpace(targetUrl))
            throw new InvalidOperationException("Target URL is required.");
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("Secret is required.");
        if (eventTypes.Count == 0)
            throw new InvalidOperationException("At least one event type is required.");
    }

    private static string SerializeEventTypes(IReadOnlyList<string> eventTypes) =>
        string.Join(",", eventTypes.Select(e => e.Trim()).Where(e => e.Length > 0));

    private static WebhookSubscriptionDto Map(WebhookSubscription entity) =>
        new()
        {
            Id = entity.Id,
            Name = entity.Name,
            TargetUrl = entity.TargetUrl,
            EventTypes = ParseEventTypes(entity.EventTypes),
            IsActive = entity.IsActive,
            CreatedAtUtc = entity.CreatedAtUtc,
        };

    internal static IReadOnlyList<string> ParseEventTypes(string raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? Array.Empty<string>()
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public sealed class WebhookPublisher : IWebhookPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ApplicationDbContext _db;
    private readonly IOutboxWriter _outbox;

    public WebhookPublisher(ApplicationDbContext db, IOutboxWriter outbox)
    {
        _db = db;
        _outbox = outbox;
    }

    public async Task EnqueueEventAsync(
        string eventType,
        object payload,
        CancellationToken cancellationToken = default)
    {
        var subscriptions = await _db.WebhookSubscriptions.AsNoTracking()
            .Where(s => s.IsActive)
            .ToListAsync(cancellationToken);

        var bodyJson = JsonSerializer.Serialize(payload, JsonOptions);

        foreach (var subscription in subscriptions)
        {
            if (!WebhookSubscriptionService.ParseEventTypes(subscription.EventTypes)
                    .Contains(eventType, StringComparer.Ordinal))
                continue;

            await _outbox.EnqueueAsync(
                OutboxMessageTypes.WebhookDeliver,
                new WebhookDeliverPayload
                {
                    SubscriptionId = subscription.Id,
                    TargetUrl = subscription.TargetUrl,
                    Secret = subscription.Secret,
                    EventType = eventType,
                    BodyJson = bodyJson,
                },
                cancellationToken);
        }
    }
}
