namespace SistemaGastos.Application.DTOs;

public class MonthlyBalanceDto
{
    public string Key { get; set; }
    public string Label { get; set; }
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Balance { get; set; }
    public string BalanceFmt { get; set; }
    public string IncomeFmt { get; set; }
    public string ExpenseFmt { get; set; }
    public decimal FixedExpenses { get; set; }
    public string FixedExpensesFmt { get; set; } = string.Empty;
    // Nuevo: resumen de cuotas pendientes para este mes (si aplica)
    public decimal PendingInstallments { get; set; }
    public string InstallmentsFmt { get; set; } = string.Empty;
    public bool HasCreditCardPayment { get; set; }
}
