namespace SistemaGastos.Application.DTOs;

public class DashboardStatsDto
{
    public decimal TotalARSValue { get; set; }
    public Dictionary<string, decimal> MonthlyBalances { get; set; }
    public List<DateTime> Months { get; set; }
    public List<TmpTransactionDto> Transactions { get; set; }
}
