namespace SistemaGastos.Application.DTOs;

public class DebtPlanDataDto
{
    public decimal CurrentBalance { get; set; }
    public string CurrentBalanceFmt { get; set; } = string.Empty;
    public decimal DolarRate { get; set; }
    public List<DebtPlanMonthBaseDto> Months { get; set; } = new();
    /// <summary>Gastos fijos activos disponibles para simular su eliminación.</summary>
    public List<FixedExpenseSimDto> FixedExpenses { get; set; } = new();
    /// <summary>Última configuración guardada por el usuario (o valores por defecto).</summary>
    public DebtPlanSettingsDto Settings { get; set; } = new();
}
