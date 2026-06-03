namespace SistemaGastos.Application.DTOs;

public class MonthlyBalanceDto
{
    public string Key { get; set; }
    public string Label { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }

    public decimal Income { get; set; }
    public string IncomeFmt { get; set; }

    /// <summary>Solo gastos manuales + fijos (excluye cuotas TC).</summary>
    public decimal Expense { get; set; }
    public string ExpenseFmt { get; set; }

    public decimal ManualExpenses { get; set; }
    public string ManualExpensesFmt { get; set; } = string.Empty;

    public decimal FixedExpenses { get; set; }
    public string FixedExpensesFmt { get; set; } = string.Empty;

    public decimal PendingInstallments { get; set; }
    public string InstallmentsFmt { get; set; } = string.Empty;

    public decimal Balance { get; set; }
    public string BalanceFmt { get; set; }

    public bool HasCreditCardPayment { get; set; }
    public decimal PersonsReceivable { get; set; }
    public string PersonsReceivableFmt { get; set; } = string.Empty;
}
