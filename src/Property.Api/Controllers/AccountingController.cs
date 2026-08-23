using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Accounting;
using Property.Domain.Enums;

namespace Property.Api.Controllers;

[ApiController]
[RequireTenantModule(TenantModuleKeys.Accounting)]
[Route("api/accounting")]
public sealed class AccountingController : ControllerBase
{
    private readonly IChartOfAccountsService _coa;
    private readonly IJournalService _journals;
    private readonly IBudgetService _budgets;
    private readonly IAccountsPayableService _ap;
    private readonly IPettyCashService _pettyCash;
    private readonly IWithholdingTaxService _tax;
    private readonly IAccountingPeriodService _periods;
    private readonly IBankReconciliationService _bank;

    public AccountingController(
        IChartOfAccountsService coa,
        IJournalService journals,
        IBudgetService budgets,
        IAccountsPayableService ap,
        IPettyCashService pettyCash,
        IWithholdingTaxService tax,
        IAccountingPeriodService periods,
        IBankReconciliationService bank)
    {
        _coa = coa;
        _journals = journals;
        _budgets = budgets;
        _ap = ap;
        _pettyCash = pettyCash;
        _tax = tax;
        _periods = periods;
        _bank = bank;
    }

    [HttpGet("coa")]
    [RequireFeature(FeatureCodes.AccountingCoa)]
    public async Task<ActionResult<IReadOnlyList<LedgerAccountDto>>> ListCoa(
        CancellationToken cancellationToken = default) =>
        Ok(await _coa.ListAsync(cancellationToken));

    [HttpPut("coa/{id:guid}")]
    [RequireFeature(FeatureCodes.AccountingCoa)]
    public async Task<ActionResult<LedgerAccountDto>> UpdateCoa(
        Guid id, [FromBody] UpdateLedgerAccountDto dto, CancellationToken cancellationToken = default)
    {
        var item = await _coa.UpdateAsync(id, dto, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("journals")]
    [RequireFeature(FeatureCodes.AccountingJournals)]
    public async Task<ActionResult<IReadOnlyList<JournalEntryDto>>> ListJournals(
        [FromQuery] Guid horizontalPropertyId,
        [FromQuery] JournalSourceType? sourceType = null,
        CancellationToken cancellationToken = default) =>
        Ok(await _journals.ListAsync(horizontalPropertyId, sourceType, cancellationToken));

    [HttpGet("budgets")]
    [RequireFeature(FeatureCodes.AccountingBudget)]
    public async Task<ActionResult<IReadOnlyList<BudgetDto>>> ListBudgets(
        [FromQuery] Guid horizontalPropertyId,
        CancellationToken cancellationToken = default) =>
        Ok(await _budgets.ListAsync(horizontalPropertyId, cancellationToken));

    [HttpPost("budgets")]
    [RequireFeature(FeatureCodes.AccountingBudget)]
    public async Task<ActionResult<BudgetDto>> CreateBudget(
        [FromBody] CreateBudgetDto dto, CancellationToken cancellationToken = default) =>
        Ok(await _budgets.CreateAsync(dto, cancellationToken));

    [HttpGet("budgets/execution")]
    [RequireFeature(FeatureCodes.AccountingBudget)]
    public async Task<ActionResult<IReadOnlyList<BudgetExecutionRowDto>>> BudgetExecution(
        [FromQuery] Guid horizontalPropertyId,
        [FromQuery] int year,
        CancellationToken cancellationToken = default) =>
        Ok(await _budgets.GetExecutionAsync(horizontalPropertyId, year, cancellationToken));

    [HttpGet("vendors")]
    [RequireFeature(FeatureCodes.AccountingAp)]
    public async Task<ActionResult<IReadOnlyList<VendorDto>>> ListVendors(
        CancellationToken cancellationToken = default) =>
        Ok(await _ap.ListVendorsAsync(cancellationToken));

    [HttpPost("vendors")]
    [RequireFeature(FeatureCodes.AccountingAp)]
    public async Task<ActionResult<VendorDto>> CreateVendor(
        [FromBody] CreateVendorDto dto, CancellationToken cancellationToken = default) =>
        Ok(await _ap.CreateVendorAsync(dto, cancellationToken));

    [HttpGet("ap-invoices")]
    [RequireFeature(FeatureCodes.AccountingAp)]
    public async Task<ActionResult<IReadOnlyList<ApInvoiceDto>>> ListApInvoices(
        [FromQuery] Guid horizontalPropertyId,
        CancellationToken cancellationToken = default) =>
        Ok(await _ap.ListInvoicesAsync(horizontalPropertyId, cancellationToken));

    [HttpPost("ap-invoices")]
    [RequireFeature(FeatureCodes.AccountingAp)]
    public async Task<ActionResult<ApInvoiceDto>> CreateApInvoice(
        [FromBody] CreateApInvoiceDto dto, CancellationToken cancellationToken = default) =>
        Ok(await _ap.CreateInvoiceAsync(dto, cancellationToken));

    [HttpPost("ap-payments")]
    [RequireFeature(FeatureCodes.AccountingAp)]
    public async Task<ActionResult<ApPaymentDto>> CreateApPayment(
        [FromBody] CreateApPaymentDto dto, CancellationToken cancellationToken = default) =>
        Ok(await _ap.CreatePaymentAsync(dto, cancellationToken));

    [HttpGet("petty-cash")]
    [RequireFeature(FeatureCodes.AccountingPettyCash)]
    public async Task<ActionResult<IReadOnlyList<PettyCashFundDto>>> ListPettyCash(
        [FromQuery] Guid horizontalPropertyId,
        CancellationToken cancellationToken = default) =>
        Ok(await _pettyCash.ListFundsAsync(horizontalPropertyId, cancellationToken));

    [HttpPost("petty-cash")]
    [RequireFeature(FeatureCodes.AccountingPettyCash)]
    public async Task<ActionResult<PettyCashFundDto>> CreatePettyCashFund(
        [FromBody] CreatePettyCashFundDto dto, CancellationToken cancellationToken = default) =>
        Ok(await _pettyCash.CreateFundAsync(dto, cancellationToken));

    [HttpPost("petty-cash/expenses")]
    [RequireFeature(FeatureCodes.AccountingPettyCash)]
    public async Task<ActionResult<PettyCashExpenseDto>> CreatePettyCashExpense(
        [FromBody] CreatePettyCashExpenseDto dto, CancellationToken cancellationToken = default) =>
        Ok(await _pettyCash.CreateExpenseAsync(dto, cancellationToken));

    [HttpGet("tax/rates")]
    [RequireFeature(FeatureCodes.AccountingTax)]
    public async Task<ActionResult<IReadOnlyList<WithholdingTaxRateDto>>> ListTaxRates(
        CancellationToken cancellationToken = default) =>
        Ok(await _tax.ListRatesAsync(cancellationToken));

    [HttpPut("tax/rates")]
    [RequireFeature(FeatureCodes.AccountingTax)]
    public async Task<ActionResult<WithholdingTaxRateDto>> UpsertTaxRate(
        [FromBody] UpsertWithholdingTaxRateDto dto, CancellationToken cancellationToken = default) =>
        Ok(await _tax.UpsertRateAsync(dto, cancellationToken));

    [HttpGet("tax/export")]
    [RequireFeature(FeatureCodes.AccountingTax)]
    public async Task<ActionResult<IReadOnlyList<WithholdingExportRowDto>>> ExportTax(
        [FromQuery] Guid horizontalPropertyId,
        [FromQuery] int year,
        [FromQuery] int? month = null,
        CancellationToken cancellationToken = default) =>
        Ok(await _tax.ExportAsync(horizontalPropertyId, year, month, cancellationToken));

    [HttpGet("periods")]
    [RequireFeature(FeatureCodes.AccountingReports)]
    public async Task<ActionResult<IReadOnlyList<AccountingPeriodDto>>> ListPeriods(
        [FromQuery] Guid horizontalPropertyId,
        CancellationToken cancellationToken = default) =>
        Ok(await _periods.ListAsync(horizontalPropertyId, cancellationToken));

    [HttpPost("periods/close")]
    [RequireFeature(FeatureCodes.AccountingReports)]
    public async Task<ActionResult<AccountingPeriodDto>> ClosePeriod(
        [FromBody] ClosePeriodDto dto, CancellationToken cancellationToken = default) =>
        Ok(await _periods.CloseAsync(dto, cancellationToken));

    [HttpGet("reports/statements")]
    [RequireFeature(FeatureCodes.AccountingReports)]
    public async Task<ActionResult<FinancialStatementsDto>> Statements(
        [FromQuery] Guid horizontalPropertyId,
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken = default) =>
        Ok(await _periods.GetStatementsAsync(horizontalPropertyId, year, month, cancellationToken));

    [HttpGet("bank-statements")]
    [RequireFeature(FeatureCodes.AccountingBank)]
    public async Task<ActionResult<IReadOnlyList<BankStatementDto>>> ListBankStatements(
        [FromQuery] Guid horizontalPropertyId,
        CancellationToken cancellationToken = default) =>
        Ok(await _bank.ListStatementsAsync(horizontalPropertyId, cancellationToken));

    [HttpPost("bank-statements")]
    [RequireFeature(FeatureCodes.AccountingBank)]
    public async Task<ActionResult<BankStatementDto>> CreateBankStatement(
        [FromBody] CreateBankStatementDto dto, CancellationToken cancellationToken = default) =>
        Ok(await _bank.CreateStatementAsync(dto, cancellationToken));

    [HttpPost("bank-statements/match")]
    [RequireFeature(FeatureCodes.AccountingBank)]
    public async Task<ActionResult<BankStatementLineDto>> MatchBankLine(
        [FromBody] MatchBankLineDto dto, CancellationToken cancellationToken = default) =>
        Ok(await _bank.MatchAsync(dto, cancellationToken));
}
