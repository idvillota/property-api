using Property.Domain.Enums;

namespace Property.Application.Features.Accounting;

public sealed class LedgerAccountDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public LedgerAccountType AccountType { get; set; }
    public string? ParentCode { get; set; }
    public bool IsPostable { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }
}

public sealed class UpdateLedgerAccountDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class JournalEntryDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public DateTime EntryDate { get; set; }
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public JournalSourceType SourceType { get; set; }
    public Guid? SourceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public IReadOnlyList<JournalLineDto> Lines { get; set; } = [];
}

public sealed class JournalLineDto
{
    public Guid Id { get; set; }
    public Guid LedgerAccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Memo { get; set; }
}

public sealed class BudgetDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public int Year { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public IReadOnlyList<BudgetLineDto> Lines { get; set; } = [];
}

public sealed class BudgetLineDto
{
    public Guid Id { get; set; }
    public Guid LedgerAccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public int Month { get; set; }
    public decimal Amount { get; set; }
}

public sealed class CreateBudgetDto
{
    public Guid HorizontalPropertyId { get; set; }
    public int Year { get; set; }
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<CreateBudgetLineDto> Lines { get; set; } = [];
}

public sealed class CreateBudgetLineDto
{
    public Guid LedgerAccountId { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
}

public sealed class BudgetExecutionRowDto
{
    public Guid LedgerAccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public int Month { get; set; }
    public decimal BudgetAmount { get; set; }
    public decimal ExecutedAmount { get; set; }
    public decimal Variance { get; set; }
}

public sealed class VendorDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CreateVendorDto
{
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string? Email { get; set; }
}

public sealed class ApInvoiceDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public Guid ExpenseAccountId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal WithholdingAmount { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }
    public ApInvoiceStatus Status { get; set; }
    public WithholdingKind? WithholdingKind { get; set; }
}

public sealed class CreateApInvoiceDto
{
    public Guid HorizontalPropertyId { get; set; }
    public Guid VendorId { get; set; }
    public Guid ExpenseAccountId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal Subtotal { get; set; }
    public WithholdingKind? WithholdingKind { get; set; }
}

public sealed class CreateApPaymentDto
{
    public Guid ApInvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public PaymentMethod Method { get; set; }
    public string? Reference { get; set; }
}

public sealed class ApPaymentDto
{
    public Guid Id { get; set; }
    public Guid ApInvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public PaymentMethod Method { get; set; }
    public string? Reference { get; set; }
}

public sealed class PettyCashFundDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CreatePettyCashFundDto
{
    public Guid HorizontalPropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
}

public sealed class CreatePettyCashExpenseDto
{
    public Guid PettyCashFundId { get; set; }
    public Guid? ExpenseAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime SpentAt { get; set; }
    public Guid SupportFileAssetId { get; set; }
}

public sealed class PettyCashExpenseDto
{
    public Guid Id { get; set; }
    public Guid PettyCashFundId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime SpentAt { get; set; }
    public Guid SupportFileAssetId { get; set; }
}

public sealed class WithholdingTaxRateDto
{
    public Guid Id { get; set; }
    public WithholdingKind Kind { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal RatePercent { get; set; }
    public decimal MinimumBase { get; set; }
    public bool IsActive { get; set; }
}

public sealed class UpsertWithholdingTaxRateDto
{
    public WithholdingKind Kind { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal RatePercent { get; set; }
    public decimal MinimumBase { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class WithholdingExportRowDto
{
    public Guid ApInvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public WithholdingKind Kind { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal WithholdingAmount { get; set; }
    public DateTime IssueDate { get; set; }
}

public sealed class AccountingPeriodDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public AccountingPeriodStatus Status { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
}

public sealed class ClosePeriodDto
{
    public Guid HorizontalPropertyId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
}

public sealed class FinancialStatementRowDto
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public LedgerAccountType AccountType { get; set; }
    public decimal Balance { get; set; }
}

public sealed class FinancialStatementsDto
{
    public Guid HorizontalPropertyId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilitiesEquity { get; set; }
    public bool IsBalanced { get; set; }
    public IReadOnlyList<FinancialStatementRowDto> Rows { get; set; } = [];
}

public sealed class BankStatementDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public string AccountLabel { get; set; } = string.Empty;
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public IReadOnlyList<BankStatementLineDto> Lines { get; set; } = [];
}

public sealed class BankStatementLineDto
{
    public Guid Id { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public BankLineMatchStatus MatchStatus { get; set; }
    public Guid? MatchedPaymentId { get; set; }
}

public sealed class CreateBankStatementDto
{
    public Guid HorizontalPropertyId { get; set; }
    public string AccountLabel { get; set; } = string.Empty;
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public IReadOnlyList<CreateBankStatementLineDto> Lines { get; set; } = [];
}

public sealed class CreateBankStatementLineDto
{
    public DateOnly TransactionDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed class MatchBankLineDto
{
    public Guid BankStatementLineId { get; set; }
    public Guid PaymentId { get; set; }
}
