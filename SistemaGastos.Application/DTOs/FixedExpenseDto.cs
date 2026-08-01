namespace SistemaGastos.Application.DTOs;

public class FixedExpenseDto
{
    public int ID { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AmountFormatted { get; set; } = string.Empty;
    public string Currency { get; set; } = "ARS";
    public int PaymentDay { get; set; }
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int AccountID { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public bool Active { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? LastGeneratedDate { get; set; }
    public bool AlreadyPaidThisMonth { get; set; }
    public string? PaidMonthName { get; set; }
    /// <summary>Monto real abonado en el mes (tomado de la Transaction generada al pagar).</summary>
    public decimal? PaidAmount { get; set; }
    public string? PaidAmountFormatted { get; set; }
    public int? PersonID { get; set; }
    public decimal? PersonPercentage { get; set; }
    public string? PersonName { get; set; }
    public int? DistributionEndDay { get; set; }
    public string? ExcludedDays { get; set; }
    public bool IsPausedThisMonth { get; set; }
    public string? PausedMonths { get; set; }
    public int? CreditCardAccountID { get; set; }
    public string? PaymentYearMonth { get; set; }
    public bool IsCreditCardPayment => CreditCardAccountID.HasValue;
    public decimal? TcMinimumAmount { get; set; }
    public decimal? TcTotalAmount { get; set; }
    public bool IsSystemGenerated { get; set; }
}
