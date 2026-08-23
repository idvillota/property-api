using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Accounting;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services.Accounting;

public sealed class BudgetService : IBudgetService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenant;

    public BudgetService(ApplicationDbContext db, ICurrentTenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<BudgetDto> CreateAsync(CreateBudgetDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenant();
        var budget = new Budget
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            HorizontalPropertyId = dto.HorizontalPropertyId,
            Year = dto.Year,
            Name = dto.Name.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        foreach (var line in dto.Lines)
        {
            if (line.Month is < 1 or > 12)
                throw new InvalidOperationException("Budget month must be 1..12.");
            budget.Lines.Add(new BudgetLine
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                BudgetId = budget.Id,
                LedgerAccountId = line.LedgerAccountId,
                Month = line.Month,
                Amount = Math.Round(line.Amount, 2, MidpointRounding.AwayFromZero),
                CreatedAtUtc = DateTime.UtcNow,
            });
        }

        await _db.Budgets.AddAsync(budget, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return (await ListAsync(dto.HorizontalPropertyId, cancellationToken)).First(b => b.Id == budget.Id);
    }

    public async Task<IReadOnlyList<BudgetDto>> ListAsync(
        Guid horizontalPropertyId, CancellationToken cancellationToken = default)
    {
        var list = await _db.Budgets.AsNoTracking()
            .Include(b => b.Lines).ThenInclude(l => l.LedgerAccount)
            .Where(b => b.HorizontalPropertyId == horizontalPropertyId && b.IsActive)
            .OrderByDescending(b => b.Year)
            .ToListAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<BudgetExecutionRowDto>> GetExecutionAsync(
        Guid horizontalPropertyId, int year, CancellationToken cancellationToken = default)
    {
        var budget = await _db.Budgets.AsNoTracking()
            .Include(b => b.Lines).ThenInclude(l => l.LedgerAccount)
            .Where(b => b.HorizontalPropertyId == horizontalPropertyId && b.Year == year && b.IsActive)
            .OrderByDescending(b => b.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (budget is null)
            return [];

        var accountIds = budget.Lines.Select(l => l.LedgerAccountId).Distinct().ToList();
        var executed = await (
            from l in _db.JournalLines.AsNoTracking()
            join e in _db.JournalEntries.AsNoTracking() on l.JournalEntryId equals e.Id
            where accountIds.Contains(l.LedgerAccountId)
                  && e.HorizontalPropertyId == horizontalPropertyId
                  && e.PeriodYear == year
            group l by new { l.LedgerAccountId, e.PeriodMonth } into g
            select new
            {
                g.Key.LedgerAccountId,
                Month = g.Key.PeriodMonth,
                Amount = g.Sum(x => x.Debit - x.Credit),
            }).ToListAsync(cancellationToken);
        var execMap = executed.ToDictionary(x => (x.LedgerAccountId, x.Month), x => x.Amount);

        return budget.Lines.Select(l =>
        {
            var executedAmt = Math.Round(execMap.GetValueOrDefault((l.LedgerAccountId, l.Month)), 2);
            // Expense accounts: debit increases execution; for income use absolute credit
            if (l.LedgerAccount?.AccountType == LedgerAccountType.Income)
                executedAmt = Math.Abs(executedAmt);
            else if (executedAmt < 0)
                executedAmt = Math.Abs(executedAmt);

            return new BudgetExecutionRowDto
            {
                LedgerAccountId = l.LedgerAccountId,
                AccountCode = l.LedgerAccount?.Code ?? string.Empty,
                AccountName = l.LedgerAccount?.Name ?? string.Empty,
                Month = l.Month,
                BudgetAmount = l.Amount,
                ExecutedAmount = executedAmt,
                Variance = Math.Round(l.Amount - executedAmt, 2),
            };
        }).OrderBy(r => r.AccountCode).ThenBy(r => r.Month).ToList();
    }

    private Guid RequireTenant() =>
        _tenant.TenantId is Guid id && id != Guid.Empty
            ? id
            : throw new InvalidOperationException("Tenant context is required.");

    private static BudgetDto Map(Budget b) => new()
    {
        Id = b.Id,
        HorizontalPropertyId = b.HorizontalPropertyId,
        Year = b.Year,
        Name = b.Name,
        IsActive = b.IsActive,
        Lines = b.Lines.Select(l => new BudgetLineDto
        {
            Id = l.Id,
            LedgerAccountId = l.LedgerAccountId,
            AccountCode = l.LedgerAccount?.Code ?? string.Empty,
            AccountName = l.LedgerAccount?.Name ?? string.Empty,
            Month = l.Month,
            Amount = l.Amount,
        }).ToList(),
    };
}
