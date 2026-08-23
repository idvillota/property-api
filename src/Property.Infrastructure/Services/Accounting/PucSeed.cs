using Property.Domain.Enums;

namespace Property.Infrastructure.Services.Accounting;

internal static class PucSeed
{
    public static readonly (string Code, string Name, LedgerAccountType Type, string? Parent, bool Postable)[] Accounts =
    [
        ("1", "Activo", LedgerAccountType.Asset, null, false),
        ("11", "Disponible", LedgerAccountType.Asset, "1", false),
        ("1105", "Caja", LedgerAccountType.Asset, "11", true),
        ("1110", "Bancos", LedgerAccountType.Asset, "11", true),
        ("13", "Deudores", LedgerAccountType.Asset, "1", false),
        ("1305", "Clientes — cartera de cuotas", LedgerAccountType.Asset, "13", true),
        ("2", "Pasivo", LedgerAccountType.Liability, null, false),
        ("22", "Proveedores", LedgerAccountType.Liability, "2", false),
        ("2205", "Cuentas por pagar", LedgerAccountType.Liability, "22", true),
        ("23", "Impuestos", LedgerAccountType.Liability, "2", false),
        ("2365", "Retención en la fuente por pagar", LedgerAccountType.Liability, "23", true),
        ("2408", "ICA por pagar", LedgerAccountType.Liability, "23", true),
        ("3", "Patrimonio", LedgerAccountType.Equity, null, false),
        ("3115", "Aportes / patrimonio PH", LedgerAccountType.Equity, "3", true),
        ("4", "Ingresos", LedgerAccountType.Income, null, false),
        ("41", "Operacionales", LedgerAccountType.Income, "4", false),
        ("4135", "Cuotas de administración", LedgerAccountType.Income, "41", true),
        ("5", "Gastos", LedgerAccountType.Expense, null, false),
        ("51", "Operacionales de administración", LedgerAccountType.Expense, "5", false),
        ("5105", "Gastos de administración", LedgerAccountType.Expense, "51", true),
        ("5195", "Gastos de caja menor", LedgerAccountType.Expense, "51", true),
    ];

    public const string Cash = "1105";
    public const string Bank = "1110";
    public const string Receivable = "1305";
    public const string Payable = "2205";
    public const string WithholdingPayable = "2365";
    public const string IcaPayable = "2408";
    public const string IncomeAdmin = "4135";
    public const string ExpenseAdmin = "5105";
    public const string ExpensePetty = "5195";
}
