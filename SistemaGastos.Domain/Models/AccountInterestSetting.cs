namespace SistemaGastos.Domain.Models;

/// <summary>
/// Marca una cuenta para el cálculo automático de intereses diarios sobre su saldo
/// (ej: descubierto bancario) y define la tasa a aplicar.
/// </summary>
public class AccountInterestSetting
{
    public int ID { get; set; }
    public int AccountID { get; set; }
    public int UserID { get; set; }

    /// <summary>Tasa anual aplicada en la fórmula (saldo * Rate * días) / 365.</summary>
    public decimal InterestRate { get; set; } = 1.55m;

    public bool Enabled { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
    public virtual Login User { get; set; } = null!;
}
