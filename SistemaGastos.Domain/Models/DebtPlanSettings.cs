namespace SistemaGastos.Domain.Models;

/// <summary>
/// Persiste la última configuración utilizada por el usuario en el
/// simulador "Plan de Metas Financieras" (Proyecciones), para restaurarla
/// automáticamente cuando vuelve a entrar al módulo.
/// </summary>
public class DebtPlanSettings
{
    public int ID { get; set; }
    public int UserID { get; set; }

    /// <summary>"maxNegative" | "reachZero" | "reachPositive".</summary>
    public string GoalType { get; set; } = "maxNegative";
    public decimal GoalValue { get; set; } = 1500000;

    /// <summary>Ingreso extra mensual estimado (modo simple).</summary>
    public decimal ExtraMonthlyIncome { get; set; }

    /// <summary>true = modo "3 escenarios" activo.</summary>
    public bool ScenariosMode { get; set; }
    public decimal ScenarioMin { get; set; } = 150000;
    public decimal ScenarioNormal { get; set; } = 350000;
    public decimal ScenarioMax { get; set; } = 600000;

    /// <summary>IDs de FixedExpense seleccionados para simular su corte, separados por coma.</summary>
    public string? RemovedFixedExpenseIds { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Login User { get; set; } = null!;
}
