namespace SistemaGastos.Domain.Models;

/// <summary>
/// Registra el cobro mensual (día 7) de los intereses devengados el mes anterior,
/// evitando que se cree más de una transacción por cuenta y mes.
/// </summary>
public class AccountInterestMonthlyCharge
{
    public int ID { get; set; }
    public int AccountID { get; set; }
    public int UserID { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }

    public decimal TotalInterest { get; set; }

    public int? TransactionID { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
    public virtual Login User { get; set; } = null!;
    public virtual Transaction? Transaction { get; set; }
}
