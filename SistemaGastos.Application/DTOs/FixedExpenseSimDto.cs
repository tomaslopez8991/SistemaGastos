namespace SistemaGastos.Application.DTOs;

public class FixedExpenseSimDto
{
    public int ID { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal MonthlyAmountARS { get; set; }
    public string MonthlyAmountARSFmt { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    /// <summary>Índices 0-11 de los meses donde este gasto aplica en la proyección.</summary>
    public List<int> ActiveMonthIndices { get; set; } = new();
}
