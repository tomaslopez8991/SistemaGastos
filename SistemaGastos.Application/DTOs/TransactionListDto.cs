namespace SistemaGastos.Application.DTOs;

public class TransactionListDto
{
    public List<TransactionDTO> Transactions { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
