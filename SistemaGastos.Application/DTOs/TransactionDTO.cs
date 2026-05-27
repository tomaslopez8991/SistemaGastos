namespace SistemaGastos.Application.DTOs;

public class TransactionDTO
{
    public int ID { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }

    public string CategoryType { get; set; }
    public bool IsIncome { get; set; }

    public int AccountID { get; set; }
    public string AccountName { get; set; }
    public string AccountCurrency { get; set; }

    public int CategoryID { get; set; }
    public string CategoryName { get; set; }

    public string Description { get; set; }
    public int? FixedExpenseID { get; set; }
    public string FixedExpenseName { get; set; }
    public string AmountFormatted { get; set; }
    public string DateFormatted { get; set; }
}
