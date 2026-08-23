using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Property.Api.Authorization;
using Property.Api.Controllers;
using Property.Application.Authorization;
using Property.Application.Common.Options;
using Property.Application.Features.Accounting;
using Property.Application.Features.Billing.FeeCharges;
using Property.Application.Features.Billing.Payments;
using Property.Application.Features.Organization.TenantSettings;
using Property.Application.Messaging;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Auditing;
using Property.Infrastructure.Authorization;
using Property.Infrastructure.Messaging;
using Property.Infrastructure.Services;
using Property.Infrastructure.Services.Accounting;
using Property.Tests.Unit.Support;
using DomainUnit = Property.Domain.Entities.Unit;

namespace Property.Tests.Unit.Infrastructure.Services.Accounting;

public sealed class AccountingModuleTests
{
    [Fact]
    public async Task Coa_Seed_OnModuleEnable()
    {
        using var fx = new TenantDbFixture();
        var coa = new ChartOfAccountsService(fx.Db, fx.TenantContext);
        var modules = new TenantModuleService(fx.Db, fx.TenantContext, coa);

        await modules.UpdateFlagsAsync(new TenantModuleFlagsDto { Accounting = true, Portal = true });

        var accounts = await fx.Db.LedgerAccounts.IgnoreQueryFilters()
            .Where(a => a.TenantId == fx.TenantId)
            .ToListAsync();
        Assert.Contains(accounts, a => a.Code == "1305");
        Assert.Contains(accounts, a => a.Code == "4135");
    }

    [Fact]
    public void Coa_Forbidden_WhenModuleOff()
    {
        var attr = typeof(AccountingController).GetCustomAttribute<RequireTenantModuleAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(TenantModuleKeys.Accounting, attr!.ModuleKey);
    }

    [Fact]
    public void AccountantRole_HasAccountingFeatures()
    {
        var perms = FeatureCatalog.DefaultFeaturesByRole[SystemRoles.Accountant];
        foreach (var code in FeatureCodes.AccountingAll)
            Assert.Contains(code, perms);
    }

    [Fact]
    public void AccountantRole_Denied_OrgRoles()
    {
        var perms = FeatureCatalog.DefaultFeaturesByRole[SystemRoles.Accountant];
        Assert.DoesNotContain(FeatureCodes.OrganizationRoles, perms);
        Assert.Contains(SystemRoles.Accountant, SystemRoles.All);
    }

    [Fact]
    public async Task Journal_FromCharge_IsBalanced()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedPropertyAsync(fx);
        await EnableAccountingAsync(fx);

        var handler = new ChargePostedOutboxHandler(
            fx.Db,
            new JournalService(fx.Db, fx.TenantContext, new ChartOfAccountsService(fx.Db, fx.TenantContext)),
            new TenantModuleService(fx.Db, fx.TenantContext, new ChartOfAccountsService(fx.Db, fx.TenantContext)));

        var payload = System.Text.Json.JsonSerializer.Serialize(new ChargePostedPayload
        {
            HorizontalPropertyId = seed.Property.Id,
            BillingPeriodId = Guid.NewGuid(),
            FeeChargeIds = [seed.Charge.Id],
        }, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });

        await handler.HandleAsync(payload);

        var entry = await fx.Db.JournalEntries.Include(e => e.Lines)
            .FirstAsync(e => e.SourceType == JournalSourceType.FeeCharge && e.SourceId == seed.Charge.Id);
        Assert.Equal(entry.Lines.Sum(l => l.Debit), entry.Lines.Sum(l => l.Credit));
        Assert.Equal(seed.Charge.Amount, entry.Lines.Sum(l => l.Debit));
    }

    [Fact]
    public async Task Journal_FromPayment_IsBalanced()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedPropertyAsync(fx);
        await EnableAccountingAsync(fx);

        var outbox = new OutboxWriter(fx.Db, fx.TenantContext, Microsoft.Extensions.Options.Options.Create(new OutboxOptions()));
        var payments = new PaymentService(
            fx.Db,
            fx.Repository<Payment>(),
            fx.Repository<FeeCharge>(),
            fx.Repository<DomainUnit>(),
            fx.Repository<HorizontalProperty>(),
            fx.Repository<Person>(),
            fx.UnitOfWork,
            new AuditWriter(fx.Db, fx.TenantContext),
            outbox);

        var payment = await payments.CreateAsync(new CreatePaymentDto
        {
            HorizontalPropertyId = seed.Property.Id,
            UnitId = seed.Unit.Id,
            Amount = seed.Charge.Amount,
            PaidAt = DateTime.UtcNow,
            Method = PaymentMethod.Transfer,
            Allocations = [new CreatePaymentAllocationDto { FeeChargeId = seed.Charge.Id, Amount = seed.Charge.Amount }],
        });

        var msg = await fx.Db.OutboxMessages.FirstAsync(m => m.MessageType == OutboxMessageTypes.PaymentPosted);
        var handler = new PaymentPostedOutboxHandler(
            fx.Db,
            new JournalService(fx.Db, fx.TenantContext, new ChartOfAccountsService(fx.Db, fx.TenantContext)),
            new TenantModuleService(fx.Db, fx.TenantContext, new ChartOfAccountsService(fx.Db, fx.TenantContext)));
        await handler.HandleAsync(msg.PayloadJson);

        var entry = await fx.Db.JournalEntries.Include(e => e.Lines)
            .FirstAsync(e => e.SourceType == JournalSourceType.Payment && e.SourceId == payment.Id);
        Assert.Equal(entry.Lines.Sum(l => l.Debit), entry.Lines.Sum(l => l.Credit));
    }

    [Fact]
    public async Task Budget_Execution_AggregatesJournals()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedPropertyAsync(fx);
        await EnableAccountingAsync(fx);
        var coa = new ChartOfAccountsService(fx.Db, fx.TenantContext);
        await coa.EnsureSeededAsync();
        var expense = await fx.Db.LedgerAccounts.FirstAsync(a => a.Code == "5105");
        var journals = new JournalService(fx.Db, fx.TenantContext, coa);
        await journals.PostBalancedAsync(
            seed.Property.Id,
            new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            JournalSourceType.Manual,
            null,
            "Gasto prueba",
            [("5105", 50_000m, 0m, null), ("1105", 0m, 50_000m, null)]);

        var budgets = new BudgetService(fx.Db, fx.TenantContext);
        await budgets.CreateAsync(new CreateBudgetDto
        {
            HorizontalPropertyId = seed.Property.Id,
            Year = 2026,
            Name = "Presupuesto 2026",
            Lines =
            [
                new CreateBudgetLineDto { LedgerAccountId = expense.Id, Month = 3, Amount = 100_000m },
            ],
        });

        var exec = await budgets.GetExecutionAsync(seed.Property.Id, 2026);
        var row = Assert.Single(exec);
        Assert.Equal(100_000m, row.BudgetAmount);
        Assert.Equal(50_000m, row.ExecutedAmount);
        Assert.Equal(50_000m, row.Variance);
    }

    [Fact]
    public async Task ApInvoice_CreatesJournal()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedPropertyAsync(fx);
        await EnableAccountingAsync(fx);
        var coa = new ChartOfAccountsService(fx.Db, fx.TenantContext);
        await coa.EnsureSeededAsync();
        var expense = await fx.Db.LedgerAccounts.FirstAsync(a => a.Code == "5105");
        var ap = new AccountsPayableService(
            fx.Db, fx.TenantContext, new JournalService(fx.Db, fx.TenantContext, coa));
        var vendor = await ap.CreateVendorAsync(new CreateVendorDto { Name = "Proveedor SA" });
        var invoice = await ap.CreateInvoiceAsync(new CreateApInvoiceDto
        {
            HorizontalPropertyId = seed.Property.Id,
            VendorId = vendor.Id,
            ExpenseAccountId = expense.Id,
            InvoiceNumber = "F-1",
            IssueDate = DateTime.UtcNow,
            Subtotal = 200_000m,
        });

        var entry = await fx.Db.JournalEntries.Include(e => e.Lines)
            .FirstAsync(e => e.SourceType == JournalSourceType.ApInvoice && e.SourceId == invoice.Id);
        Assert.Equal(entry.Lines.Sum(l => l.Debit), entry.Lines.Sum(l => l.Credit));
    }

    [Fact]
    public async Task ApPayment_ReducesBalance()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedPropertyAsync(fx);
        await EnableAccountingAsync(fx);
        var coa = new ChartOfAccountsService(fx.Db, fx.TenantContext);
        await coa.EnsureSeededAsync();
        var expense = await fx.Db.LedgerAccounts.FirstAsync(a => a.Code == "5105");
        var ap = new AccountsPayableService(
            fx.Db, fx.TenantContext, new JournalService(fx.Db, fx.TenantContext, coa));
        var vendor = await ap.CreateVendorAsync(new CreateVendorDto { Name = "Proveedor SA" });
        var invoice = await ap.CreateInvoiceAsync(new CreateApInvoiceDto
        {
            HorizontalPropertyId = seed.Property.Id,
            VendorId = vendor.Id,
            ExpenseAccountId = expense.Id,
            InvoiceNumber = "F-2",
            IssueDate = DateTime.UtcNow,
            Subtotal = 100_000m,
        });

        await ap.CreatePaymentAsync(new CreateApPaymentDto
        {
            ApInvoiceId = invoice.Id,
            Amount = 40_000m,
            PaidAt = DateTime.UtcNow,
            Method = PaymentMethod.Transfer,
        });

        var updated = (await ap.ListInvoicesAsync(seed.Property.Id)).First(i => i.Id == invoice.Id);
        Assert.Equal(60_000m, updated.Balance);
        Assert.Equal(ApInvoiceStatus.Partial, updated.Status);
    }

    [Fact]
    public async Task PettyCash_Expense_RequiresAttachment()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedPropertyAsync(fx);
        await EnableAccountingAsync(fx);
        var coa = new ChartOfAccountsService(fx.Db, fx.TenantContext);
        var petty = new PettyCashService(fx.Db, fx.TenantContext, new JournalService(fx.Db, fx.TenantContext, coa));
        var fund = await petty.CreateFundAsync(new CreatePettyCashFundDto
        {
            HorizontalPropertyId = seed.Property.Id,
            Name = "Caja",
            OpeningBalance = 500_000m,
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => petty.CreateExpenseAsync(new CreatePettyCashExpenseDto
        {
            PettyCashFundId = fund.Id,
            Amount = 10_000m,
            Description = "Sin soporte",
            SpentAt = DateTime.UtcNow,
            SupportFileAssetId = Guid.Empty,
        }));
    }

    [Fact]
    public async Task PettyCash_Balance_Updates()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedPropertyAsync(fx);
        await EnableAccountingAsync(fx);
        var coa = new ChartOfAccountsService(fx.Db, fx.TenantContext);
        await coa.EnsureSeededAsync();
        var file = new FileAsset
        {
            Id = Guid.NewGuid(),
            TenantId = fx.TenantId,
            FileName = "soporte.pdf",
            ContentType = "application/pdf",
            SizeBytes = 10,
            StorageKey = "t/soporte.pdf",
            CreatedAtUtc = DateTime.UtcNow,
        };
        fx.Db.FileAssets.Add(file);
        await fx.Db.SaveChangesAsync();

        var petty = new PettyCashService(fx.Db, fx.TenantContext, new JournalService(fx.Db, fx.TenantContext, coa));
        var fund = await petty.CreateFundAsync(new CreatePettyCashFundDto
        {
            HorizontalPropertyId = seed.Property.Id,
            Name = "Caja",
            OpeningBalance = 100_000m,
        });
        await petty.CreateExpenseAsync(new CreatePettyCashExpenseDto
        {
            PettyCashFundId = fund.Id,
            Amount = 25_000m,
            Description = "Papelería",
            SpentAt = DateTime.UtcNow,
            SupportFileAssetId = file.Id,
        });

        var updated = (await petty.ListFundsAsync(seed.Property.Id)).First(f => f.Id == fund.Id);
        Assert.Equal(75_000m, updated.CurrentBalance);
    }

    [Fact]
    public async Task Withholding_Calculated_OnApInvoice()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedPropertyAsync(fx);
        await EnableAccountingAsync(fx);
        var coa = new ChartOfAccountsService(fx.Db, fx.TenantContext);
        await coa.EnsureSeededAsync();
        var tax = new WithholdingTaxService(fx.Db, fx.TenantContext);
        await tax.UpsertRateAsync(new UpsertWithholdingTaxRateDto
        {
            Kind = WithholdingKind.ReteFte,
            Name = "ReteFuente",
            RatePercent = 11m,
            MinimumBase = 0m,
        });
        var expense = await fx.Db.LedgerAccounts.FirstAsync(a => a.Code == "5105");
        var ap = new AccountsPayableService(
            fx.Db, fx.TenantContext, new JournalService(fx.Db, fx.TenantContext, coa));
        var vendor = await ap.CreateVendorAsync(new CreateVendorDto { Name = "Proveedor" });
        var invoice = await ap.CreateInvoiceAsync(new CreateApInvoiceDto
        {
            HorizontalPropertyId = seed.Property.Id,
            VendorId = vendor.Id,
            ExpenseAccountId = expense.Id,
            InvoiceNumber = "F-WH",
            IssueDate = DateTime.UtcNow,
            Subtotal = 100_000m,
            WithholdingKind = WithholdingKind.ReteFte,
        });
        Assert.Equal(11_000m, invoice.WithholdingAmount);
        Assert.Equal(89_000m, invoice.Total);
    }

    [Fact]
    public async Task Withholding_Export_ContainsRows()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedPropertyAsync(fx);
        await EnableAccountingAsync(fx);
        var coa = new ChartOfAccountsService(fx.Db, fx.TenantContext);
        await coa.EnsureSeededAsync();
        var tax = new WithholdingTaxService(fx.Db, fx.TenantContext);
        await tax.UpsertRateAsync(new UpsertWithholdingTaxRateDto
        {
            Kind = WithholdingKind.ReteFte,
            Name = "ReteFuente",
            RatePercent = 10m,
            MinimumBase = 0m,
        });
        var expense = await fx.Db.LedgerAccounts.FirstAsync(a => a.Code == "5105");
        var ap = new AccountsPayableService(
            fx.Db, fx.TenantContext, new JournalService(fx.Db, fx.TenantContext, coa));
        var vendor = await ap.CreateVendorAsync(new CreateVendorDto { Name = "Proveedor" });
        await ap.CreateInvoiceAsync(new CreateApInvoiceDto
        {
            HorizontalPropertyId = seed.Property.Id,
            VendorId = vendor.Id,
            ExpenseAccountId = expense.Id,
            InvoiceNumber = "F-EX",
            IssueDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            Subtotal = 50_000m,
            WithholdingKind = WithholdingKind.ReteFte,
        });

        var rows = await tax.ExportAsync(seed.Property.Id, 2026, 5);
        Assert.NotEmpty(rows);
    }

    [Fact]
    public async Task Period_Close_BlocksJournals()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedPropertyAsync(fx);
        await EnableAccountingAsync(fx);
        var periods = new AccountingPeriodService(fx.Db, fx.TenantContext);
        await periods.CloseAsync(new ClosePeriodDto
        {
            HorizontalPropertyId = seed.Property.Id,
            Year = 2026,
            Month = 4,
        });

        var journals = new JournalService(fx.Db, fx.TenantContext, new ChartOfAccountsService(fx.Db, fx.TenantContext));
        await Assert.ThrowsAsync<InvalidOperationException>(() => journals.PostBalancedAsync(
            seed.Property.Id,
            new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            JournalSourceType.Manual,
            null,
            "Bloqueado",
            [("1105", 1m, 0m, null), ("1110", 0m, 1m, null)]));
    }

    [Fact]
    public async Task FinancialStatements_Balance()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedPropertyAsync(fx);
        await EnableAccountingAsync(fx);
        var coa = new ChartOfAccountsService(fx.Db, fx.TenantContext);
        var journals = new JournalService(fx.Db, fx.TenantContext, coa);
        await journals.PostBalancedAsync(
            seed.Property.Id,
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            JournalSourceType.Manual,
            null,
            "Aporte",
            [("1105", 1_000_000m, 0m, null), ("3115", 0m, 1_000_000m, null)]);

        var statements = await new AccountingPeriodService(fx.Db, fx.TenantContext)
            .GetStatementsAsync(seed.Property.Id, 2026, 6);
        Assert.True(statements.IsBalanced);
        Assert.Equal(statements.TotalAssets, statements.TotalLiabilitiesEquity);
    }

    [Fact]
    public async Task BankRec_Match_Payment()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedPropertyAsync(fx);
        await EnableAccountingAsync(fx);
        var outbox = new OutboxWriter(fx.Db, fx.TenantContext, Microsoft.Extensions.Options.Options.Create(new OutboxOptions()));
        var payments = new PaymentService(
            fx.Db,
            fx.Repository<Payment>(),
            fx.Repository<FeeCharge>(),
            fx.Repository<DomainUnit>(),
            fx.Repository<HorizontalProperty>(),
            fx.Repository<Person>(),
            fx.UnitOfWork,
            new AuditWriter(fx.Db, fx.TenantContext),
            outbox);
        var payment = await payments.CreateAsync(new CreatePaymentDto
        {
            HorizontalPropertyId = seed.Property.Id,
            UnitId = seed.Unit.Id,
            Amount = seed.Charge.Amount,
            PaidAt = DateTime.UtcNow,
            Method = PaymentMethod.Transfer,
            Allocations = [new CreatePaymentAllocationDto { FeeChargeId = seed.Charge.Id, Amount = seed.Charge.Amount }],
        });

        var bank = new BankReconciliationService(fx.Db, fx.TenantContext);
        var statement = await bank.CreateStatementAsync(new CreateBankStatementDto
        {
            HorizontalPropertyId = seed.Property.Id,
            AccountLabel = "Bancolombia",
            PeriodFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
            PeriodTo = DateOnly.FromDateTime(DateTime.UtcNow),
            Lines =
            [
                new CreateBankStatementLineDto
                {
                    TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Description = "Abono",
                    Amount = seed.Charge.Amount,
                },
            ],
        });

        var matched = await bank.MatchAsync(new MatchBankLineDto
        {
            BankStatementLineId = statement.Lines[0].Id,
            PaymentId = payment.Id,
        });
        Assert.Equal(BankLineMatchStatus.Matched, matched.MatchStatus);
        Assert.Equal(payment.Id, matched.MatchedPaymentId);
    }

    [Fact]
    public async Task BankRec_Unmatched_Remains()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedPropertyAsync(fx);
        var bank = new BankReconciliationService(fx.Db, fx.TenantContext);
        var statement = await bank.CreateStatementAsync(new CreateBankStatementDto
        {
            HorizontalPropertyId = seed.Property.Id,
            AccountLabel = "Bancolombia",
            PeriodFrom = new DateOnly(2026, 1, 1),
            PeriodTo = new DateOnly(2026, 1, 31),
            Lines =
            [
                new CreateBankStatementLineDto
                {
                    TransactionDate = new DateOnly(2026, 1, 15),
                    Description = "Sin match",
                    Amount = 12_345m,
                },
            ],
        });
        Assert.Equal(BankLineMatchStatus.Unmatched, statement.Lines[0].MatchStatus);
    }

    private static async Task EnableAccountingAsync(TenantDbFixture fx)
    {
        var coa = new ChartOfAccountsService(fx.Db, fx.TenantContext);
        await new TenantModuleService(fx.Db, fx.TenantContext, coa)
            .UpdateFlagsAsync(new TenantModuleFlagsDto { Accounting = true, Portal = true });
    }

    private static async Task<(HorizontalProperty Property, DomainUnit Unit, FeeCharge Charge, Person Person)> SeedPropertyAsync(
        TenantDbFixture fx)
    {
        var property = new HorizontalProperty
        {
            Id = Guid.NewGuid(),
            Name = "PH Test",
            AddressLine = "1",
            City = "Pasto",
            Country = "CO",
            IsActive = true,
        };
        var unit = new DomainUnit
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Code = "101",
            UnitType = UnitType.Apartment,
            Coefficient = 1m,
            IsActive = true,
            IsBillable = true,
        };
        var person = new Person
        {
            Id = Guid.NewGuid(),
            DocumentType = DocumentType.Cc,
            DocumentNumber = "1",
            FirstName = "Ana",
            LastName = "Pérez",
            IsActive = true,
        };
        var period = new BillingPeriod
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Year = 2026,
            Month = 8,
            Status = BillingPeriodStatus.Open,
        };
        var charge = new FeeCharge
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            BillingPeriodId = period.Id,
            UnitId = unit.Id,
            ChargeType = FeeChargeType.Ordinary,
            Description = "Cuota",
            CoefficientSnapshot = 1m,
            Amount = 150_000m,
            AmountPaid = 0,
            DueDate = new DateOnly(2026, 8, 1),
            Status = FeeChargeStatus.Pending,
            IsActive = true,
        };
        fx.Db.HorizontalProperties.Add(property);
        fx.Db.Units.Add(unit);
        fx.Db.People.Add(person);
        fx.Db.BillingPeriods.Add(period);
        fx.Db.FeeCharges.Add(charge);
        await fx.Db.SaveChangesAsync();
        return (property, unit, charge, person);
    }
}
