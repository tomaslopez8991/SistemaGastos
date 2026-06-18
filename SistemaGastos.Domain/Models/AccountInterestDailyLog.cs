namespace SistemaGastos.Domain.Models;

/// <summary>
/// Registro diario del saldo de una cuenta y el interés devengado ese día
/// (|saldo| * tasa * DayCounter / 365). El contador de días se reinicia
/// cada vez que cambia el saldo de la cuenta.
/// </summary>
public class AccountInterestDailyLog
{
    public int ID { get; set; }
    public int AccountID { get; set; }
    public int UserID { get; set; }

    [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
    public DateTime Date { get; set; }

    /// <summary>Saldo de la cuenta utilizado para el cálculo de ese día.</summary>
    public decimal Balance { get; set; }

    /// <summary>Cantidad de días consecutivos que la cuenta lleva con ese saldo.</summary>
    public int DayCounter { get; set; }

    /// <summary>Interés devengado ese día (crece con DayCounter mientras el saldo no cambie).</summary>
    public decimal DailyInterest { get; set; }

    /// <summary>Suma de DailyInterest desde el inicio del log hasta este registro (inclusive).</summary>
    public decimal CumulativeInterest { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
    public virtual Login User { get; set; } = null!;
}
