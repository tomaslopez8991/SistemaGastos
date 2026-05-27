namespace SistemaGastos.Application.DTOs;

public class TmpTransactionListDto
{
    public List<TmpTransactionDto> TmpTransactions { get; set; } = new();
    public decimal CurrentBalance { get; set; }
    public decimal ProjectedIncome { get; set; }
    public decimal ProjectedExpense { get; set; }
    public decimal FixedExpenses { get; set; }
    public decimal PendingInstallments { get; set; }
    public decimal ProjectedBalance { get; set; }
    public bool HasCreditCardPayment { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }

    public bool IsCurrentMonth { get; set; }
    public bool IsFutureMonth { get; set; }
    public bool IsPastMonth { get; set; }

    public BalanceBreakdownDto? BalanceBreakdown { get; set; }
}
