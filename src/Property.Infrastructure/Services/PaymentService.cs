using Microsoft.EntityFrameworkCore;
using Property.Application.Auditing;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Billing.Payments;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Common;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _db;
    private readonly IRepository<Payment> _payments;
    private readonly IRepository<FeeCharge> _charges;
    private readonly IRepository<Unit> _units;
    private readonly IRepository<HorizontalProperty> _properties;
    private readonly IRepository<Person> _people;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditWriter _audit;

    public PaymentService(
        ApplicationDbContext db,
        IRepository<Payment> payments,
        IRepository<FeeCharge> charges,
        IRepository<Unit> units,
        IRepository<HorizontalProperty> properties,
        IRepository<Person> people,
        IUnitOfWork unitOfWork,
        IAuditWriter audit)
    {
        _db = db;
        _payments = payments;
        _charges = charges;
        _units = units;
        _properties = properties;
        _people = people;
        _unitOfWork = unitOfWork;
        _audit = audit;
    }

    public Task<PagedResult<PaymentDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default) =>
        ListQueryHelpers.ToPagedResultAsync(
            _payments.Query().AsNoTracking().Include(p => p.Allocations),
            query,
            q => PagedEntityQueries.ShapePayments(q, query),
            entities => Task.FromResult<IReadOnlyList<PaymentDto>>(entities.Select(Map).ToList()),
            cancellationToken);

    public async Task<PaymentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _payments.Query().AsNoTracking()
            .Include(p => p.Allocations)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<PaymentDto> CreateAsync(CreatePaymentDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Allocations.Count == 0)
            throw new InvalidOperationException("At least one allocation is required.");

        var allocationSum = Math.Round(dto.Allocations.Sum(a => a.Amount), 2, MidpointRounding.AwayFromZero);
        var amount = Math.Round(dto.Amount, 2, MidpointRounding.AwayFromZero);
        if (allocationSum != amount)
            throw new InvalidOperationException("Sum of allocations must equal the payment amount.");

        var propertyExists = await _properties.Query().AnyAsync(p => p.Id == dto.HorizontalPropertyId, cancellationToken);
        if (!propertyExists)
            throw new InvalidOperationException("Horizontal property was not found.");

        var unit = await _units.Query().AsNoTracking().FirstOrDefaultAsync(u => u.Id == dto.UnitId, cancellationToken)
            ?? throw new InvalidOperationException("Unit was not found.");
        if (unit.HorizontalPropertyId != dto.HorizontalPropertyId)
            throw new InvalidOperationException("Unit does not belong to the horizontal property.");

        if (dto.PersonId.HasValue)
        {
            var personExists = await _people.Query().AnyAsync(p => p.Id == dto.PersonId.Value, cancellationToken);
            if (!personExists)
                throw new InvalidOperationException("Person was not found.");
        }

        var chargeIds = dto.Allocations.Select(a => a.FeeChargeId).Distinct().ToList();
        if (chargeIds.Count != dto.Allocations.Count)
            throw new InvalidOperationException("Duplicate fee charge allocations are not allowed.");

        // Retry strategy requires the whole user transaction to run as one retriable unit.
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            var charges = await _charges.Query()
                .Where(c => chargeIds.Contains(c.Id))
                .ToListAsync(cancellationToken);
            if (charges.Count != chargeIds.Count)
                throw new InvalidOperationException("One or more fee charges were not found.");

            foreach (var charge in charges)
            {
                if (!charge.IsActive)
                    throw new InvalidOperationException("Inactive fee charges cannot receive payments.");
                if (charge.HorizontalPropertyId != dto.HorizontalPropertyId || charge.UnitId != dto.UnitId)
                    throw new InvalidOperationException("Fee charges must belong to the same unit and horizontal property.");
                if (charge.Status is FeeChargeStatus.Waived or FeeChargeStatus.Cancelled or FeeChargeStatus.Paid)
                    throw new InvalidOperationException("Fee charge is not open for payment.");
            }

            await using var transaction = _db.Database.IsRelational()
                ? await _db.Database.BeginTransactionAsync(cancellationToken)
                : null;
            try
            {
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    HorizontalPropertyId = dto.HorizontalPropertyId,
                    UnitId = dto.UnitId,
                    PersonId = dto.PersonId,
                    Amount = amount,
                    PaidAt = dto.PaidAt.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(dto.PaidAt, DateTimeKind.Utc)
                        : dto.PaidAt.ToUniversalTime(),
                    Method = dto.Method,
                    Reference = dto.Reference?.Trim(),
                    Notes = dto.Notes?.Trim(),
                    IsActive = true,
                };

                var chargesById = charges.ToDictionary(c => c.Id);
                foreach (var allocationDto in dto.Allocations)
                {
                    var charge = chargesById[allocationDto.FeeChargeId];
                    var allocationAmount = Math.Round(allocationDto.Amount, 2, MidpointRounding.AwayFromZero);
                    var remaining = charge.Amount - charge.AmountPaid;
                    if (allocationAmount > remaining)
                        throw new InvalidOperationException("Allocation exceeds the remaining balance of a fee charge.");

                    charge.AmountPaid = Math.Round(charge.AmountPaid + allocationAmount, 2, MidpointRounding.AwayFromZero);
                    charge.Status = charge.AmountPaid >= charge.Amount
                        ? FeeChargeStatus.Paid
                        : FeeChargeStatus.Partial;
                    _charges.Update(charge);

                    payment.Allocations.Add(new PaymentAllocation
                    {
                        Id = Guid.NewGuid(),
                        FeeChargeId = charge.Id,
                        Amount = allocationAmount,
                    });
                }

                await _payments.AddAsync(payment, cancellationToken);
                await _audit.WriteAsync(
                    AuditEntityTypes.Payment,
                    payment.Id.ToString(),
                    AuditActions.PaymentCreated,
                    before: null,
                    after: new
                    {
                        payment.Id,
                        payment.HorizontalPropertyId,
                        payment.UnitId,
                        payment.PersonId,
                        payment.Amount,
                        payment.Method,
                        payment.Reference,
                        allocations = payment.Allocations.Select(a => new { a.FeeChargeId, a.Amount }),
                    },
                    cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                if (transaction is not null)
                    await transaction.CommitAsync(cancellationToken);

                return Map(payment);
            }
            catch
            {
                if (transaction is not null)
                    await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            var payment = await _payments.Query()
                .Include(p => p.Allocations)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (payment is null || !payment.IsActive)
                return false;

            await using var transaction = _db.Database.IsRelational()
                ? await _db.Database.BeginTransactionAsync(cancellationToken)
                : null;
            try
            {
                var chargeIds = payment.Allocations.Select(a => a.FeeChargeId).Distinct().ToList();
                var charges = await _charges.Query()
                    .Where(c => chargeIds.Contains(c.Id))
                    .ToListAsync(cancellationToken);
                var chargesById = charges.ToDictionary(c => c.Id);

                foreach (var allocation in payment.Allocations)
                {
                    if (!chargesById.TryGetValue(allocation.FeeChargeId, out var charge))
                        throw new InvalidOperationException("A fee charge for payment reversal was not found.");

                    charge.AmountPaid = Math.Round(
                        Math.Max(0m, charge.AmountPaid - allocation.Amount),
                        2,
                        MidpointRounding.AwayFromZero);

                    if (charge.Status is not (FeeChargeStatus.Waived or FeeChargeStatus.Cancelled))
                    {
                        charge.Status = charge.AmountPaid <= 0m
                            ? FeeChargeStatus.Pending
                            : charge.AmountPaid >= charge.Amount
                                ? FeeChargeStatus.Paid
                                : FeeChargeStatus.Partial;
                    }

                    _charges.Update(charge);
                }

                payment.IsActive = false;
                _payments.Update(payment);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                if (transaction is not null)
                    await transaction.CommitAsync(cancellationToken);
                return true;
            }
            catch
            {
                if (transaction is not null)
                    await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    private static PaymentDto Map(Payment entity) =>
        new()
        {
            Id = entity.Id,
            HorizontalPropertyId = entity.HorizontalPropertyId,
            UnitId = entity.UnitId,
            PersonId = entity.PersonId,
            Amount = entity.Amount,
            PaidAt = entity.PaidAt,
            Method = entity.Method,
            Reference = entity.Reference,
            Notes = entity.Notes,
            IsActive = entity.IsActive,
            Allocations = entity.Allocations
                .Select(a => new PaymentAllocationDto
                {
                    Id = a.Id,
                    FeeChargeId = a.FeeChargeId,
                    Amount = a.Amount,
                })
                .ToList(),
        };
}
