namespace SistemaGastos.Application.DTOs;

/// <summary>
/// Última configuración guardada por el usuario en el simulador "Plan de Metas Financieras".
/// Se usa tanto para devolver el estado guardado como para recibir el guardado automático.
/// </summary>
public class DebtPlanSettingsDto
{
    /// <summary>"maxNegative" | "reachZero" | "reachPositive".</summary>
    public string GoalType { get; set; } = "maxNegative";
    public decimal GoalValue { get; set; } = 1500000;
    public decimal ExtraMonthlyIncome { get; set; }
    public bool ScenariosMode { get; set; }
    public decimal ScenarioMin { get; set; } = 150000;
    public decimal ScenarioNormal { get; set; } = 350000;
    public decimal ScenarioMax { get; set; } = 600000;
    public List<int> RemovedFixedExpenseIds { get; set; } = new();
}
