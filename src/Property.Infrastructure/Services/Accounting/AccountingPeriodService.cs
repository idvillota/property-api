using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Accounting;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services.Accounting;

public sealed class AccountingPeriodService : IAccountingPeriodService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenant;

    public AccountingPeriodService(ApplicationDbContext db, ICurrentTenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<AccountingPeriodDto> CloseAsync(
        ClosePeriodDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Month is < 1 or > 12)
            throw new InvalidOperationException("Mes inválido.");

        var period = await _db.AccountingPeriods.FirstOrDefaultAsync(
            p => p.HorizontalPropertyId == dto.HorizontalPropertyId
                 && p.Year == dto.Year
                 && p.Month == dto.Month,
            cancellationToken);

        if (period is null)
        {
            period = new AccountingPeriod
            {
                Id = Guid.NewGuid(),
                TenantId = RequireTenant(),
                HorizontalPropertyId = dto.HorizontalPropertyId,
                Year = dto.Year,
                Month = dto.Month,
                CreatedAtUtc = DateTime.UtcNow,
            };
            await _db.AccountingPeriods.AddAsync(period, cancellationToken);
        }

        if (period.Status == AccountingPeriodStatus.Closed)
            throw new InvalidOperationException("El período ya está cerrado.");

        period.Status = AccountingPeriodStatus.Closed;
        period.ClosedAtUtc = DateTime.UtcNow;
        period.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Map(period);
    }

    public async Task<IReadOnlyList<AccountingPeriodDto>> ListAsync(
        Guid horizontalPropertyId, CancellationToken cancellationToken = default)
    {
        var rows = await _db.AccountingPeriods.AsNoTracking()
            .Where(p => p.HorizontalPropertyId == horizontalPropertyId)
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<FinancialStatementsDto> GetStatementsAsync(
        Guid horizontalPropertyId, int year, int month, CancellationToken cancellationToken = default)
    {
        var lines = await _db.JournalLines.AsNoTracking()
            .Include(l => l.LedgerAccount)
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry!.HorizontalPropertyId == horizontalPropertyId
                        && (l.JournalEntry.PeriodYear < year
                            || (l.JournalEntry.PeriodYear == year && l.JournalEntry.PeriodMonth <= month)))
            .ToListAsync(cancellationToken);

        var byAccount = lines.GroupBy(l => l.LedgerAccountId).Select(g =>
        {
            var acc = g.First().LedgerAccount!;
            var debit = g.Sum(x => x.Debit);
            var credit = g.Sum(x => x.Credit);
            var balance = acc.AccountType switch
            {
                LedgerAccountType.Asset or LedgerAccountType.Expense => debit - credit,
                _ => credit - debit,
            };
            return new FinancialStatementRowDto
            {
                AccountCode = acc.Code,
                AccountName = acc.Name,
                AccountType = acc.AccountType,
                Balance = Math.Round(balance, 2),
            };
        }).Where(r => r.Balance != 0).OrderBy(r => r.AccountCode).ToList();

        var assets = byAccount.Where(r => r.AccountType == LedgerAccountType.Asset).Sum(r => r.Balance);
        var liabilities = byAccount.Where(r => r.AccountType == LedgerAccountType.Liability).Sum(r => r.Balance);
        var equity = byAccount.Where(r => r.AccountType == LedgerAccountType.Equity).Sum(r => r.Balance);
        var income = byAccount.Where(r => r.AccountType == LedgerAccountType.Income).Sum(r => r.Balance);
        var expense = byAccount.Where(r => r.AccountType == LedgerAccountType.Expense).Sum(r => r.Balance);
        var netIncome = income - expense;
        var totalLE = Math.Round(liabilities + equity + netIncome, 2);
        var totalAssets = Math.Round(assets, 2);

        return new FinancialStatementsDto
        {
            HorizontalPropertyId = horizontalPropertyId,
            Year = year,
            Month = month,
            TotalAssets = totalAssets,
            TotalLiabilitiesEquity = totalLE,
            IsBalanced = totalAssets == totalLE,
            Rows = byAccount,
        };
    }

    private Guid RequireTenant() =>
        _tenant.TenantId is Guid id && id != Guid.Empty
            ? id
            : throw new InvalidOperationException("Tenant context is required.");

    private static AccountingPeriodDto Map(AccountingPeriod p) => new()
    {
        Id = p.Id,
        HorizontalPropertyId = p.HorizontalPropertyId,
        Year = p.Year,
        Month = p.Month,
        Status = p.Status,
        ClosedAtUtc = p.ClosedAtUtc,
    };
}
