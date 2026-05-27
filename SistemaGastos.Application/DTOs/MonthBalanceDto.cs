namespace SistemaGastos.Application.DTOs;

public class MonthBalanceDto
{
    public string Key { get; set; }
    public string Label { get; set; }

    public decimal Balance { get; set; }
    public string BalanceFmt { get; set; }

    public decimal Income { get; set; }
    public string IncomeFmt { get; set; }

    public decimal Expense { get; set; }
    public string ExpenseFmt { get; set; }

    public decimal PendingInstallments { get; set; }
    public string InstallmentsFmt { get; set; }
    public bool HasCreditCardPayment { get; set; }

    // (Opcional por ahora) Si después vas a armar el "Ver Desglose" completo, 
    // puedes dejar esto preparado:
    // public BalanceBreakdownDto BalanceBreakdown { get; set; } 
}