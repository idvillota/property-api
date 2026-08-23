using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Accounting;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services.Accounting;

public sealed class BankReconciliationService : IBankReconciliationService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenant;

    public BankReconciliationService(ApplicationDbContext db, ICurrentTenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<BankStatementDto> CreateStatementAsync(
        CreateBankStatementDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenant();
        var statement = new BankStatement
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            HorizontalPropertyId = dto.HorizontalPropertyId,
            AccountLabel = dto.AccountLabel.Trim(),
            PeriodFrom = dto.PeriodFrom,
            PeriodTo = dto.PeriodTo,
            CreatedAtUtc = DateTime.UtcNow,
        };
        foreach (var line in dto.Lines)
        {
            statement.Lines.Add(new BankStatementLine
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                BankStatementId = statement.Id,
                TransactionDate = line.TransactionDate,
                Description = line.Description.Trim(),
                Amount = Math.Round(line.Amount, 2, MidpointRounding.AwayFromZero),
                MatchStatus = BankLineMatchStatus.Unmatched,
                CreatedAtUtc = DateTime.UtcNow,
            });
        }

        await _db.BankStatements.AddAsync(statement, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(statement);
    }

    public async Task<IReadOnlyList<BankStatementDto>> ListStatementsAsync(
        Guid horizontalPropertyId, CancellationToken cancellationToken = default)
    {
        var list = await _db.BankStatements.AsNoTracking()
            .Include(s => s.Lines)
            .Where(s => s.HorizontalPropertyId == horizontalPropertyId)
            .OrderByDescending(s => s.PeriodTo)
            .ToListAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task<BankStatementLineDto> MatchAsync(
        MatchBankLineDto dto, CancellationToken cancellationToken = default)
    {
        var line = await _db.BankStatementLines.FirstOrDefaultAsync(
            l => l.Id == dto.BankStatementLineId, cancellationToken)
            ?? throw new InvalidOperationException("Línea de extracto no encontrada.");
        if (line.MatchStatus == BankLineMatchStatus.Matched)
            throw new InvalidOperationException("La línea ya está conciliada.");

        var payment = await _db.Payments.AsNoTracking().FirstOrDefaultAsync(
            p => p.Id == dto.PaymentId && p.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Pago no encontrado.");

        if (Math.Round(payment.Amount, 2) != Math.Round(Math.Abs(line.Amount), 2))
            throw new InvalidOperationException("El monto del pago no coincide con la línea del extracto.");

        line.MatchStatus = BankLineMatchStatus.Matched;
        line.MatchedPaymentId = payment.Id;
        line.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return MapLine(line);
    }

    private Guid RequireTenant() =>
        _tenant.TenantId is Guid id && id != Guid.Empty
            ? id
            : throw new InvalidOperationException("Tenant context is required.");

    private static BankStatementDto Map(BankStatement s) => new()
    {
        Id = s.Id,
        HorizontalPropertyId = s.HorizontalPropertyId,
        AccountLabel = s.AccountLabel,
        PeriodFrom = s.PeriodFrom,
        PeriodTo = s.PeriodTo,
        Lines = s.Lines.Select(MapLine).ToList(),
    };

    private static BankStatementLineDto MapLine(BankStatementLine l) => new()
    {
        Id = l.Id,
        TransactionDate = l.TransactionDate,
        Description = l.Description,
        Amount = l.Amount,
        MatchStatus = l.MatchStatus,
        MatchedPaymentId = l.MatchedPaymentId,
    };
}
