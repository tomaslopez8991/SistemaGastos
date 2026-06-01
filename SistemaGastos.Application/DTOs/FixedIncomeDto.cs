namespace SistemaGastos.Application.DTOs;

public class FixedIncomeDto
{
    public int ID { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AmountFormatted { get; set; } = string.Empty;
    public string Currency { get; set; } = "ARS";
    public int ReceiptDay { get; set; }
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int AccountID { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public bool Active { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? LastGeneratedDate { get; set; }
    public bool AlreadyReceivedThisMonth { get; set; }
    public string? ReceivedMonthName { get; set; }
}

public class FixedIncomeFormDto
{
    public FixedIncomeDto Income { get; set; } = new();
    public List<AccountDropdownDto> Accounts { get; set; } = [];
    public List<CategoryDropdownDto> Categories { get; set; } = [];
    /// <summary>Cotización dólar MEP vigente (para mostrar referencia en el form).</summary>
    public decimal DolarMep { get; set; }
}

public class ReceiptResultDto
{
    public int TransactionID { get; set; }
    public decimal Amount { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public string Message { get; set; } = string.Empty;
}
