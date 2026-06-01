namespace SistemaGastos.Application.DTOs;

public class FixedExpenseDto
{
    public int ID { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AmountFormatted { get; set; } = string.Empty;
    public int PaymentDay { get; set; }
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int AccountID { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public bool Active { get; set; }
    public DateTime? LastGeneratedDate { get; set; }
    public bool AlreadyPaidThisMonth { get; set; }
    public string? PaidMonthName { get; set; }
    public int? PersonID { get; set; }
    public string? PersonName { get; set; }
}
