using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Accounting;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services.Accounting;

public sealed class AccountsPayableService : IAccountsPayableService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenant;
    private readonly IJournalService _journals;

    public AccountsPayableService(
        ApplicationDbContext db,
        ICurrentTenantContext tenant,
        IJournalService journals)
    {
        _db = db;
        _tenant = tenant;
        _journals = journals;
    }

    public async Task<VendorDto> CreateVendorAsync(CreateVendorDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("El proveedor es obligatorio.");
        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            TenantId = RequireTenant(),
            Name = dto.Name.Trim(),
            TaxId = dto.TaxId?.Trim(),
            Email = dto.Email?.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        await _db.Vendors.AddAsync(vendor, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return MapVendor(vendor);
    }

    public async Task<IReadOnlyList<VendorDto>> ListVendorsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.Vendors.AsNoTracking().Where(v => v.IsActive).OrderBy(v => v.Name)
            .ToListAsync(cancellationToken);
        return rows.Select(MapVendor).ToList();
    }

    public async Task<ApInvoiceDto> CreateInvoiceAsync(
        CreateApInvoiceDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.VendorId == Guid.Empty)
            throw new InvalidOperationException("El proveedor es obligatorio.");
        var vendor = await _db.Vendors.AsNoTracking().FirstOrDefaultAsync(v => v.Id == dto.VendorId, cancellationToken)
            ?? throw new InvalidOperationException("Proveedor no encontrado.");
        var expense = await _db.LedgerAccounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == dto.ExpenseAccountId && a.IsPostable, cancellationToken)
            ?? throw new InvalidOperationException("Cuenta de gasto no válida.");

        var subtotal = Math.Round(dto.Subtotal, 2, MidpointRounding.AwayFromZero);
        if (subtotal <= 0)
            throw new InvalidOperationException("El subtotal debe ser mayor que cero.");

        var withholding = 0m;
        if (dto.WithholdingKind.HasValue)
        {
            var rate = await _db.WithholdingTaxRates.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Kind == dto.WithholdingKind && r.IsActive, cancellationToken);
            if (rate is not null && subtotal >= rate.MinimumBase)
                withholding = Math.Round(subtotal * rate.RatePercent / 100m, 2, MidpointRounding.AwayFromZero);
        }

        var total = Math.Round(subtotal - withholding, 2, MidpointRounding.AwayFromZero);
        var tenantId = RequireTenant();
        var issueDate = Normalize(dto.IssueDate);
        var invoice = new ApInvoice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            HorizontalPropertyId = dto.HorizontalPropertyId,
            VendorId = dto.VendorId,
            ExpenseAccountId = dto.ExpenseAccountId,
            InvoiceNumber = dto.InvoiceNumber.Trim(),
            IssueDate = issueDate,
            DueDate = dto.DueDate.HasValue ? Normalize(dto.DueDate.Value) : null,
            Subtotal = subtotal,
            WithholdingAmount = withholding,
            Total = total,
            AmountPaid = 0,
            Status = ApInvoiceStatus.Open,
            WithholdingKind = dto.WithholdingKind,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        await _db.ApInvoices.AddAsync(invoice, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var whCode = dto.WithholdingKind == WithholdingKind.Ica ? PucSeed.IcaPayable : PucSeed.WithholdingPayable;
        var journalLines = new List<(string, decimal, decimal, string?)>
        {
            (expense.Code, subtotal, 0m, $"Factura {invoice.InvoiceNumber}"),
            (PucSeed.Payable, 0m, total, null),
        };
        if (withholding > 0)
            journalLines.Add((whCode, 0m, withholding, "Retención"));

        await _journals.PostBalancedAsync(
            dto.HorizontalPropertyId,
            issueDate,
            JournalSourceType.ApInvoice,
            invoice.Id,
            $"CxP {invoice.InvoiceNumber} — {vendor.Name}",
            journalLines,
            cancellationToken);

        return MapInvoice(invoice, vendor.Name);
    }

    public async Task<IReadOnlyList<ApInvoiceDto>> ListInvoicesAsync(
        Guid horizontalPropertyId, CancellationToken cancellationToken = default)
    {
        var list = await _db.ApInvoices.AsNoTracking()
            .Include(i => i.Vendor)
            .Where(i => i.HorizontalPropertyId == horizontalPropertyId && i.IsActive)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync(cancellationToken);
        return list.Select(i => MapInvoice(i, i.Vendor?.Name ?? string.Empty)).ToList();
    }

    public async Task<ApPaymentDto> CreatePaymentAsync(
        CreateApPaymentDto dto, CancellationToken cancellationToken = default)
    {
        var invoice = await _db.ApInvoices.FirstOrDefaultAsync(i => i.Id == dto.ApInvoiceId, cancellationToken)
            ?? throw new InvalidOperationException("Factura no encontrada.");
        var amount = Math.Round(dto.Amount, 2, MidpointRounding.AwayFromZero);
        var balance = Math.Round(invoice.Total - invoice.AmountPaid, 2, MidpointRounding.AwayFromZero);
        if (amount <= 0 || amount > balance)
            throw new InvalidOperationException("Monto de pago inválido para el saldo de la factura.");

        var payment = new ApPayment
        {
            Id = Guid.NewGuid(),
            TenantId = RequireTenant(),
            ApInvoiceId = invoice.Id,
            Amount = amount,
            PaidAt = Normalize(dto.PaidAt),
            Method = dto.Method,
            Reference = dto.Reference?.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        invoice.AmountPaid = Math.Round(invoice.AmountPaid + amount, 2, MidpointRounding.AwayFromZero);
        invoice.Status = invoice.AmountPaid >= invoice.Total ? ApInvoiceStatus.Paid : ApInvoiceStatus.Partial;
        invoice.UpdatedAtUtc = DateTime.UtcNow;

        await _db.ApPayments.AddAsync(payment, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        await _journals.PostBalancedAsync(
            invoice.HorizontalPropertyId,
            payment.PaidAt,
            JournalSourceType.ApPayment,
            payment.Id,
            $"Pago CxP factura {invoice.InvoiceNumber}",
            [
                (PucSeed.Payable, amount, 0m, null),
                (PucSeed.Bank, 0m, amount, null),
            ],
            cancellationToken);

        return new ApPaymentDto
        {
            Id = payment.Id,
            ApInvoiceId = payment.ApInvoiceId,
            Amount = payment.Amount,
            PaidAt = payment.PaidAt,
            Method = payment.Method,
            Reference = payment.Reference,
        };
    }

    private Guid RequireTenant() =>
        _tenant.TenantId is Guid id && id != Guid.Empty
            ? id
            : throw new InvalidOperationException("Tenant context is required.");

    private static DateTime Normalize(DateTime dt) =>
        dt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : dt.ToUniversalTime();

    private static VendorDto MapVendor(Vendor v) => new()
    {
        Id = v.Id, Name = v.Name, TaxId = v.TaxId, Email = v.Email, IsActive = v.IsActive,
    };

    private static ApInvoiceDto MapInvoice(ApInvoice i, string vendorName) => new()
    {
        Id = i.Id,
        HorizontalPropertyId = i.HorizontalPropertyId,
        VendorId = i.VendorId,
        VendorName = vendorName,
        ExpenseAccountId = i.ExpenseAccountId,
        InvoiceNumber = i.InvoiceNumber,
        IssueDate = i.IssueDate,
        DueDate = i.DueDate,
        Subtotal = i.Subtotal,
        WithholdingAmount = i.WithholdingAmount,
        Total = i.Total,
        AmountPaid = i.AmountPaid,
        Balance = Math.Round(i.Total - i.AmountPaid, 2),
        Status = i.Status,
        WithholdingKind = i.WithholdingKind,
    };
}
