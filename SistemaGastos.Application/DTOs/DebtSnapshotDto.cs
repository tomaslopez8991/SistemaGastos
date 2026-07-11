namespace SistemaGastos.Application.DTOs;

public class DebtSnapshotDto
{
    public decimal ProjectedIncomeNextMonth { get; set; }
    public decimal ProjectedSurplusNextMonth { get; set; }
    public decimal OverdraftInterestAccrued { get; set; }
    public List<PendingFixedExpenseDto> PendingFixedExpenses { get; set; } = new();
    public List<CardDebtItemDto> CardDebts { get; set; } = new();
}

public class CardDebtItemDto
{
    public int AccountID { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string Currency { get; set; } = "ARS";
    /// <summary>Deuda actual en valor absoluto, en la moneda de la cuenta.</summary>
    public decimal CurrentBalance { get; set; }
    public DateTime NextDueDate { get; set; }
    /// <summary>Cantidad de compras en cuotas todavía no saldadas.</summary>
    public int InstallmentsRemainingCount { get; set; }
    /// <summary>Suma de las cuotas futuras pendientes (no solo la próxima), en la moneda de la cuenta.</summary>
    public decimal RemainingInstallmentsTotal { get; set; }
    /// <summary>Pago mínimo del próximo vencimiento (override manual o % del saldo). Null si la tarjeta no tiene configurado ninguno de los dos.</summary>
    public decimal? MinimumPaymentDue { get; set; }
}

public class PendingFixedExpenseDto
{
    public int ID { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ARS";
    public int PaymentDay { get; set; }
}

public class DebtAdviceDto
{
    public DebtSnapshotDto Snapshot { get; set; } = new();
    public string Advice { get; set; } = string.Empty;
}
