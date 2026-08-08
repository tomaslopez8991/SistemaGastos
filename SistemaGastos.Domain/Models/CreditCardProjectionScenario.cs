using SistemaGastos.Domain.Enums;

namespace SistemaGastos.Domain.Models;

public class CreditCardProjectionScenario
{
    public int ID { get; set; }
    public int AccountID { get; set; }
    public int UserID { get; set; }
    public string YearMonth { get; set; } = string.Empty;
    public TcProjectionMode Mode { get; set; }
    public decimal? CustomAmount { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Account Account { get; set; } = null!;
}
