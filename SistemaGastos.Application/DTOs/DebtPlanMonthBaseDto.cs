namespace SistemaGastos.Application.DTOs;

public class DebtPlanMonthBaseDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal Income { get; set; }
    public decimal ManualExpenses { get; set; }
    public decimal FixedExpensesTotal { get; set; }
    public decimal PendingInstallments { get; set; }
    public decimal PersonsReceivable { get; set; }
    /// <summary>Saldo acumulado base (sin modificaciones), calculado por GetProjectedBalancesHandler.</summary>
    public decimal BalanceBase { get; set; }
}
