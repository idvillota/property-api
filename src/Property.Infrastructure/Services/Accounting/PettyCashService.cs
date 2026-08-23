using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Accounting;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services.Accounting;

public sealed class PettyCashService : IPettyCashService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenant;
    private readonly IJournalService _journals;

    public PettyCashService(ApplicationDbContext db, ICurrentTenantContext tenant, IJournalService journals)
    {
        _db = db;
        _tenant = tenant;
        _journals = journals;
    }

    public async Task<PettyCashFundDto> CreateFundAsync(
        CreatePettyCashFundDto dto, CancellationToken cancellationToken = default)
    {
        var opening = Math.Round(dto.OpeningBalance, 2, MidpointRounding.AwayFromZero);
        var fund = new PettyCashFund
        {
            Id = Guid.NewGuid(),
            TenantId = RequireTenant(),
            HorizontalPropertyId = dto.HorizontalPropertyId,
            Name = dto.Name.Trim(),
            OpeningBalance = opening,
            CurrentBalance = opening,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        await _db.PettyCashFunds.AddAsync(fund, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(fund);
    }

    public async Task<IReadOnlyList<PettyCashFundDto>> ListFundsAsync(
        Guid horizontalPropertyId, CancellationToken cancellationToken = default)
    {
        var rows = await _db.PettyCashFunds.AsNoTracking()
            .Where(f => f.HorizontalPropertyId == horizontalPropertyId && f.IsActive)
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<PettyCashExpenseDto> CreateExpenseAsync(
        CreatePettyCashExpenseDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.SupportFileAssetId == Guid.Empty)
            throw new InvalidOperationException("El soporte (adjunto) es obligatorio para legalizar caja menor.");

        var fileExists = await _db.FileAssets.AnyAsync(f => f.Id == dto.SupportFileAssetId, cancellationToken);
        if (!fileExists)
            throw new InvalidOperationException("El archivo de soporte no existe.");

        var fund = await _db.PettyCashFunds.FirstOrDefaultAsync(f => f.Id == dto.PettyCashFundId, cancellationToken)
            ?? throw new InvalidOperationException("Fondo de caja menor no encontrado.");

        var amount = Math.Round(dto.Amount, 2, MidpointRounding.AwayFromZero);
        if (amount <= 0 || amount > fund.CurrentBalance)
            throw new InvalidOperationException("Monto inválido para el saldo de caja menor.");

        string expenseCode = PucSeed.ExpensePetty;
        if (dto.ExpenseAccountId.HasValue)
        {
            var acc = await _db.LedgerAccounts.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == dto.ExpenseAccountId && a.IsPostable, cancellationToken)
                ?? throw new InvalidOperationException("Cuenta de gasto no válida.");
            expenseCode = acc.Code;
        }

        var spentAt = dto.SpentAt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dto.SpentAt, DateTimeKind.Utc)
            : dto.SpentAt.ToUniversalTime();

        var expense = new PettyCashExpense
        {
            Id = Guid.NewGuid(),
            TenantId = RequireTenant(),
            PettyCashFundId = fund.Id,
            ExpenseAccountId = dto.ExpenseAccountId,
            Amount = amount,
            Description = dto.Description.Trim(),
            SpentAt = spentAt,
            SupportFileAssetId = dto.SupportFileAssetId,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        fund.CurrentBalance = Math.Round(fund.CurrentBalance - amount, 2, MidpointRounding.AwayFromZero);
        fund.UpdatedAtUtc = DateTime.UtcNow;

        await _db.PettyCashExpenses.AddAsync(expense, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        await _journals.PostBalancedAsync(
            fund.HorizontalPropertyId,
            spentAt,
            JournalSourceType.PettyCash,
            expense.Id,
            $"Caja menor: {expense.Description}",
            [
                (expenseCode, amount, 0m, null),
                (PucSeed.Cash, 0m, amount, null),
            ],
            cancellationToken);

        return new PettyCashExpenseDto
        {
            Id = expense.Id,
            PettyCashFundId = expense.PettyCashFundId,
            Amount = expense.Amount,
            Description = expense.Description,
            SpentAt = expense.SpentAt,
            SupportFileAssetId = expense.SupportFileAssetId,
        };
    }

    private Guid RequireTenant() =>
        _tenant.TenantId is Guid id && id != Guid.Empty
            ? id
            : throw new InvalidOperationException("Tenant context is required.");

    private static PettyCashFundDto Map(PettyCashFund f) => new()
    {
        Id = f.Id,
        HorizontalPropertyId = f.HorizontalPropertyId,
        Name = f.Name,
        OpeningBalance = f.OpeningBalance,
        CurrentBalance = f.CurrentBalance,
        IsActive = f.IsActive,
    };
}
