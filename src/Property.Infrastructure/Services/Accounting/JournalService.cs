using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Accounting;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services.Accounting;

public sealed class JournalService : IJournalService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenant;
    private readonly IChartOfAccountsService _coa;

    public JournalService(
        ApplicationDbContext db,
        ICurrentTenantContext tenant,
        IChartOfAccountsService coa)
    {
        _db = db;
        _tenant = tenant;
        _coa = coa;
    }

    public async Task EnsurePeriodOpenAsync(
        Guid horizontalPropertyId, DateTime entryDate, CancellationToken cancellationToken = default)
    {
        var year = entryDate.Year;
        var month = entryDate.Month;
        var closed = await _db.AccountingPeriods.AsNoTracking().AnyAsync(
            p => p.HorizontalPropertyId == horizontalPropertyId
                 && p.Year == year
                 && p.Month == month
                 && p.Status == AccountingPeriodStatus.Closed,
            cancellationToken);
        if (closed)
            throw new InvalidOperationException(
                $"El período contable {year}-{month:D2} está cerrado; no se pueden registrar asientos.");
    }

    public async Task<JournalEntryDto> PostBalancedAsync(
        Guid horizontalPropertyId,
        DateTime entryDate,
        JournalSourceType sourceType,
        Guid? sourceId,
        string description,
        IReadOnlyList<(string AccountCode, decimal Debit, decimal Credit, string? Memo)> lines,
        CancellationToken cancellationToken = default)
    {
        if (lines.Count == 0)
            throw new InvalidOperationException("El asiento requiere al menos una línea.");

        var debit = Math.Round(lines.Sum(l => l.Debit), 2, MidpointRounding.AwayFromZero);
        var credit = Math.Round(lines.Sum(l => l.Credit), 2, MidpointRounding.AwayFromZero);
        if (debit != credit || debit <= 0m)
            throw new InvalidOperationException("El asiento debe cuadrar (débito = crédito > 0).");

        await EnsurePeriodOpenAsync(horizontalPropertyId, entryDate, cancellationToken);
        await _coa.EnsureSeededAsync(cancellationToken);

        var tenantId = RequireTenant();
        var codes = lines.Select(l => l.AccountCode).Distinct().ToList();
        var accounts = await _db.LedgerAccounts
            .Where(a => codes.Contains(a.Code) && a.IsPostable && a.IsActive)
            .ToListAsync(cancellationToken);
        if (accounts.Count != codes.Count)
            throw new InvalidOperationException("Una o más cuentas PUC no existen o no son imputables.");

        var byCode = accounts.ToDictionary(a => a.Code, StringComparer.Ordinal);
        var entryDateUtc = entryDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(entryDate, DateTimeKind.Utc)
            : entryDate.ToUniversalTime();

        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            HorizontalPropertyId = horizontalPropertyId,
            EntryDate = entryDateUtc,
            PeriodYear = entryDateUtc.Year,
            PeriodMonth = entryDateUtc.Month,
            SourceType = sourceType,
            SourceId = sourceId,
            Description = description.Trim(),
            IsPosted = true,
            CreatedAtUtc = DateTime.UtcNow,
        };

        foreach (var line in lines)
        {
            entry.Lines.Add(new JournalLine
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                JournalEntryId = entry.Id,
                LedgerAccountId = byCode[line.AccountCode].Id,
                Debit = Math.Round(line.Debit, 2, MidpointRounding.AwayFromZero),
                Credit = Math.Round(line.Credit, 2, MidpointRounding.AwayFromZero),
                Memo = line.Memo,
                CreatedAtUtc = DateTime.UtcNow,
            });
        }

        await _db.JournalEntries.AddAsync(entry, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetMappedAsync(entry.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to load journal entry.");
    }

    public async Task<IReadOnlyList<JournalEntryDto>> ListAsync(
        Guid horizontalPropertyId,
        JournalSourceType? sourceType = null,
        CancellationToken cancellationToken = default)
    {
        var q = _db.JournalEntries.AsNoTracking()
            .Include(e => e.Lines).ThenInclude(l => l.LedgerAccount)
            .Where(e => e.HorizontalPropertyId == horizontalPropertyId);
        if (sourceType.HasValue)
            q = q.Where(e => e.SourceType == sourceType.Value);
        var list = await q.OrderByDescending(e => e.EntryDate).ThenByDescending(e => e.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    private async Task<JournalEntryDto?> GetMappedAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _db.JournalEntries.AsNoTracking()
            .Include(e => e.Lines).ThenInclude(l => l.LedgerAccount)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return entry is null ? null : Map(entry);
    }

    private Guid RequireTenant()
    {
        if (_tenant.TenantId is not Guid id || id == Guid.Empty)
            throw new InvalidOperationException("Tenant context is required.");
        return id;
    }

    private static JournalEntryDto Map(JournalEntry e)
    {
        var lines = e.Lines.Select(l => new JournalLineDto
        {
            Id = l.Id,
            LedgerAccountId = l.LedgerAccountId,
            AccountCode = l.LedgerAccount?.Code ?? string.Empty,
            AccountName = l.LedgerAccount?.Name ?? string.Empty,
            Debit = l.Debit,
            Credit = l.Credit,
            Memo = l.Memo,
        }).ToList();
        return new JournalEntryDto
        {
            Id = e.Id,
            HorizontalPropertyId = e.HorizontalPropertyId,
            EntryDate = e.EntryDate,
            PeriodYear = e.PeriodYear,
            PeriodMonth = e.PeriodMonth,
            SourceType = e.SourceType,
            SourceId = e.SourceId,
            Description = e.Description,
            TotalDebit = lines.Sum(l => l.Debit),
            TotalCredit = lines.Sum(l => l.Credit),
            Lines = lines,
        };
    }
}
