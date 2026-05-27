namespace SistemaGastos.Application.DTOs;

public class TransactionFormDto
{
    public int ID { get; set; }
    public DateTime? Date { get; set; }
    public decimal Amount { get; set; }

    public int AccountID { get; set; }
    public int CategoryID { get; set; }
    public string Description { get; set; }
    public int? FixedExpenseID { get; set; }

    public List<AccountDto> Accounts { get; set; }
    public List<CategoryDropdownDto> Categories { get; set; }
    public List<FixedExpenseDropdownDto> FixedExpenses { get; set; }
}

public class FixedExpenseDropdownDto
{
    public int ID { get; set; }
    public string Name { get; set; }
    public decimal Amount { get; set; }
}
