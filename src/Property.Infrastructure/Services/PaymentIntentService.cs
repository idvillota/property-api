using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Billing.PaymentIntents;
using Property.Application.Features.Billing.Payments;
using Property.Application.Messaging;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Common;
using Property.Infrastructure.Payments;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class PaymentIntentService : IPaymentIntentService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ApplicationDbContext _db;
    private readonly IPaymentService _payments;
    private readonly IPaymentReceiptService _receipts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxWriter _outbox;

    public PaymentIntentService(
        ApplicationDbContext db,
        IPaymentService payments,
        IPaymentReceiptService receipts,
        IUnitOfWork unitOfWork,
        IOutboxWriter outbox)
    {
        _db = db;
        _payments = payments;
        _receipts = receipts;
        _unitOfWork = unitOfWork;
        _outbox = outbox;
    }

    public Task<PagedResult<PaymentIntentDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default) =>
        ListQueryHelpers.ToPagedResultAsync(
            _db.PaymentIntents.AsNoTracking(),
            query,
            q => Shape(q, query),
            entities => Task.FromResult<IReadOnlyList<PaymentIntentDto>>(entities.Select(Map).ToList()),
            cancellationToken);

    public async Task<PaymentIntentDto> CreateOrGetAsync(
        CreatePaymentIntentDto dto,
        CancellationToken cancellationToken = default)
    {
        var key = NormalizeKey(dto.IdempotencyKey);
        var existing = await _db.PaymentIntents
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.IdempotencyKey == key, cancellationToken);
        if (existing is not null)
            return Map(existing);

        ValidateAllocations(dto.Amount, dto.Allocations);

        var propertyExists = await _db.HorizontalProperties.AnyAsync(
            p => p.Id == dto.HorizontalPropertyId,
            cancellationToken);
        if (!propertyExists)
            throw new InvalidOperationException("Horizontal property was not found.");

        var unit = await _db.Units.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == dto.UnitId, cancellationToken)
            ?? throw new InvalidOperationException("Unit was not found.");
        if (unit.HorizontalPropertyId != dto.HorizontalPropertyId)
            throw new InvalidOperationException("Unit does not belong to the horizontal property.");

        if (dto.PersonId.HasValue)
        {
            var personExists = await _db.People.AnyAsync(p => p.Id == dto.PersonId.Value, cancellationToken);
            if (!personExists)
                throw new InvalidOperationException("Person was not found.");
        }

        var chargeIds = dto.Allocations.Select(a => a.FeeChargeId).Distinct().ToList();
        if (chargeIds.Count != dto.Allocations.Count)
            throw new InvalidOperationException("Duplicate fee charge allocations are not allowed.");

        var charges = await _db.FeeCharges
            .AsNoTracking()
            .Where(c => chargeIds.Contains(c.Id))
            .ToListAsync(cancellationToken);
        if (charges.Count != chargeIds.Count)
            throw new InvalidOperationException("One or more fee charges were not found.");

        foreach (var charge in charges)
        {
            if (charge.HorizontalPropertyId != dto.HorizontalPropertyId || charge.UnitId != dto.UnitId)
                throw new InvalidOperationException("Fee charges must belong to the same unit and horizontal property.");
        }

        var entity = new PaymentIntent
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = key,
            Provider = WompiPspCheckoutService.ProviderCode,
            ProviderRef = TrimOrNull(dto.ProviderRef),
            Status = PaymentIntentStatus.Pending,
            HorizontalPropertyId = dto.HorizontalPropertyId,
            UnitId = dto.UnitId,
            PersonId = dto.PersonId,
            Amount = Math.Round(dto.Amount, 2, MidpointRounding.AwayFromZero),
            AllocationsJson = JsonSerializer.Serialize(
                dto.Allocations.Select(a => new PaymentIntentAllocationDto
                {
                    FeeChargeId = a.FeeChargeId,
                    Amount = Math.Round(a.Amount, 2, MidpointRounding.AwayFromZero),
                }),
                JsonOptions),
        };

        try
        {
            await _db.PaymentIntents.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Map(entity);
        }
        catch (DbUpdateException)
        {
            _db.Entry(entity).State = EntityState.Detached;
            var raced = await _db.PaymentIntents.AsNoTracking()
                .FirstOrDefaultAsync(i => i.IdempotencyKey == key, cancellationToken);
            if (raced is not null)
                return Map(raced);
            throw;
        }
    }

    public async Task<PaymentIntentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.PaymentIntents.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<PaymentIntentDto> AttachCheckoutAsync(
        Guid id,
        string provider,
        string providerRef,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.PaymentIntents.FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Payment intent was not found.");

        if (entity.Status is PaymentIntentStatus.Paid or PaymentIntentStatus.Failed or PaymentIntentStatus.Cancelled)
            throw new InvalidOperationException($"Cannot attach checkout to {entity.Status} intent.");

        entity.Provider = string.IsNullOrWhiteSpace(provider) ? WompiPspCheckoutService.ProviderCode : provider.Trim();
        entity.ProviderRef = TrimOrNull(providerRef) ?? entity.ProviderRef;
        entity.Status = PaymentIntentStatus.RequiresAction;
        _db.PaymentIntents.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<PaymentIntentDto> MarkPaidAsync(
        Guid id,
        MarkPaymentIntentPaidDto dto,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.PaymentIntents.FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Payment intent was not found.");

        if (entity.Status == PaymentIntentStatus.Paid)
            return Map(entity);

        if (entity.Status is PaymentIntentStatus.Failed or PaymentIntentStatus.Cancelled)
            throw new InvalidOperationException($"Cannot mark {entity.Status} intent as Paid.");

        if (entity.PaymentId.HasValue)
            throw new InvalidOperationException("Payment intent already has an associated payment.");

        var allocations = DeserializeAllocations(entity.AllocationsJson);
        var paidAt = dto.PaidAtUtc ?? DateTime.UtcNow;
        if (paidAt.Kind == DateTimeKind.Unspecified)
            paidAt = DateTime.SpecifyKind(paidAt, DateTimeKind.Utc);
        else
            paidAt = paidAt.ToUniversalTime();

        var payment = await _payments.CreateAsync(
            new CreatePaymentDto
            {
                HorizontalPropertyId = entity.HorizontalPropertyId,
                UnitId = entity.UnitId,
                PersonId = entity.PersonId,
                Amount = entity.Amount,
                PaidAt = paidAt,
                Method = PaymentMethod.Pse,
                Reference = TrimOrNull(dto.ProviderRef) ?? entity.ProviderRef ?? entity.IdempotencyKey,
                Notes = $"PaymentIntent {entity.Id} ({entity.Provider})",
                Allocations = allocations
                    .Select(a => new CreatePaymentAllocationDto
                    {
                        FeeChargeId = a.FeeChargeId,
                        Amount = a.Amount,
                    })
                    .ToList(),
            },
            cancellationToken);

        try
        {
            await _receipts.EnsureReceiptAsync(payment.Id, enqueueEmailIfEnabled: true, cancellationToken);
        }
        catch
        {
            // Receipt/email must not roll back a successful payment.
        }

        entity = await _db.PaymentIntents.FirstAsync(i => i.Id == id, cancellationToken);
        if (entity.Status == PaymentIntentStatus.Paid && entity.PaymentId.HasValue)
            return Map(entity);

        entity.Status = PaymentIntentStatus.Paid;
        entity.PaymentId = payment.Id;
        entity.PaidAtUtc = paidAt;
        if (!string.IsNullOrWhiteSpace(dto.ProviderRef))
            entity.ProviderRef = dto.ProviderRef.Trim();
        entity.FailureReason = null;
        _db.PaymentIntents.Update(entity);

        await EnqueueConfirmationEmailAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<PaymentIntentDto> MarkFailedAsync(
        Guid id,
        MarkPaymentIntentFailedDto dto,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.PaymentIntents.FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Payment intent was not found.");

        if (entity.Status == PaymentIntentStatus.Paid)
            throw new InvalidOperationException("Cannot fail a Paid intent.");
        if (entity.Status == PaymentIntentStatus.Failed)
            return Map(entity);
        if (entity.Status == PaymentIntentStatus.Cancelled)
            throw new InvalidOperationException("Cannot fail a Cancelled intent.");

        entity.Status = PaymentIntentStatus.Failed;
        entity.FailureReason = string.IsNullOrWhiteSpace(dto.Reason) ? "Failed" : dto.Reason.Trim();
        _db.PaymentIntents.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<PaymentIntentDto> MarkRequiresActionAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.PaymentIntents.FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Payment intent was not found.");

        if (entity.Status is PaymentIntentStatus.Paid or PaymentIntentStatus.Failed or PaymentIntentStatus.Cancelled)
            throw new InvalidOperationException($"Cannot move {entity.Status} intent to RequiresAction.");

        entity.Status = PaymentIntentStatus.RequiresAction;
        _db.PaymentIntents.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<PaymentIntentDto> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.PaymentIntents.FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Payment intent was not found.");

        if (entity.Status == PaymentIntentStatus.Paid)
            throw new InvalidOperationException("Cannot cancel a Paid intent.");
        if (entity.Status == PaymentIntentStatus.Cancelled)
            return Map(entity);

        entity.Status = PaymentIntentStatus.Cancelled;
        _db.PaymentIntents.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    private async Task EnqueueConfirmationEmailAsync(PaymentIntent entity, CancellationToken cancellationToken)
    {
        string? email = null;
        if (entity.PersonId is Guid personId)
        {
            email = await _db.People.AsNoTracking()
                .Where(p => p.Id == personId)
                .Select(p => p.Email)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(email))
            return;

        await _outbox.EnqueueAsync(
            OutboxMessageTypes.EmailSend,
            new EmailSendPayload
            {
                ToEmail = email,
                Subject = "Confirmación de pago PSE",
                HtmlBody =
                    $"<p>Recibimos tu pago por <strong>{entity.Amount:0.00}</strong> COP.</p>" +
                    $"<p>Referencia: {entity.ProviderRef ?? entity.IdempotencyKey}</p>",
            },
            cancellationToken);
    }

    private static IQueryable<PaymentIntent> Shape(IQueryable<PaymentIntent> query, ListQuery q)
    {
        if (q.FilterValue("horizontalPropertyId") is { } hpRaw &&
            ListQueryHelpers.TryParseGuid(hpRaw, out var horizontalPropertyId))
            query = query.Where(e => e.HorizontalPropertyId == horizontalPropertyId);

        if (q.FilterValue("unitId") is { } unitRaw &&
            ListQueryHelpers.TryParseGuid(unitRaw, out var unitId))
            query = query.Where(e => e.UnitId == unitId);

        if (q.FilterValue("status") is { } statusRaw &&
            Enum.TryParse<PaymentIntentStatus>(statusRaw, ignoreCase: true, out var status))
            query = query.Where(e => e.Status == status);

        var desc = q.IsDescending;
        return (q.SortBy?.ToLowerInvariant() ?? "createdatutc") switch
        {
            "amount" => desc ? query.OrderByDescending(e => e.Amount) : query.OrderBy(e => e.Amount),
            "status" => desc ? query.OrderByDescending(e => e.Status) : query.OrderBy(e => e.Status),
            _ => desc ? query.OrderByDescending(e => e.CreatedAtUtc) : query.OrderBy(e => e.CreatedAtUtc),
        };
    }

    private static void ValidateAllocations(decimal amount, IReadOnlyList<CreatePaymentIntentAllocationDto> allocations)
    {
        if (allocations.Count == 0)
            throw new InvalidOperationException("At least one allocation is required.");

        var allocationSum = Math.Round(allocations.Sum(a => a.Amount), 2, MidpointRounding.AwayFromZero);
        var rounded = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (allocationSum != rounded)
            throw new InvalidOperationException("Sum of allocations must equal the payment amount.");
    }

    private static string NormalizeKey(string key)
    {
        var trimmed = key.Trim();
        if (trimmed.Length == 0)
            throw new InvalidOperationException("Idempotency key is required.");
        return trimmed;
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IReadOnlyList<PaymentIntentAllocationDto> DeserializeAllocations(string json)
    {
        var list = JsonSerializer.Deserialize<List<PaymentIntentAllocationDto>>(json, JsonOptions);
        return list ?? [];
    }

    private static PaymentIntentDto Map(PaymentIntent entity) =>
        new()
        {
            Id = entity.Id,
            IdempotencyKey = entity.IdempotencyKey,
            ProviderRef = entity.ProviderRef,
            Provider = entity.Provider,
            Status = entity.Status,
            HorizontalPropertyId = entity.HorizontalPropertyId,
            UnitId = entity.UnitId,
            PersonId = entity.PersonId,
            Amount = entity.Amount,
            PaymentId = entity.PaymentId,
            FailureReason = entity.FailureReason,
            PaidAtUtc = entity.PaidAtUtc,
            CreatedAtUtc = entity.CreatedAtUtc,
            Allocations = DeserializeAllocations(entity.AllocationsJson),
        };
}
