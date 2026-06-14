namespace SistemaGastos.Application.DTOs;

public class TmpTransactionDto
{
    public long ID { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    /// <summary>"ARS" o "USD". El Amount está en la moneda original.</summary>
    public string Currency { get; set; } = "ARS";
    public int CategoryID { get; set; }
    public string CategoryName { get; set; }
    public string CategoryType { get; set; } // "Ingreso" o "Gasto"
    public int? AccountID { get; set; }
    public string AccountName { get; set; }
    public DateTime? DateTransaction { get; set; }
    public bool EsRecurrente { get; set; }
    public string AmountFormatted { get; set; }
    public bool IsIngreso { get; set; }
    public bool IsPaid { get; set; }
    /// <summary>Día del mes (1-31) hasta el cual se reparte el monto. Null = sin distribución.</summary>
    public int? DistributionEndDay { get; set; }
    /// <summary>Días del mes (separados por coma) sin movimiento dentro del rango de distribución.</summary>
    public string? ExcludedDays { get; set; }
    /// <summary>Día del mes que representa esta fila (para ConfirmDay).</summary>
    public int Day { get; set; }
    /// <summary>Indica si este ítem fue repartido en varios días.</summary>
    public bool IsDistributed { get; set; }
}