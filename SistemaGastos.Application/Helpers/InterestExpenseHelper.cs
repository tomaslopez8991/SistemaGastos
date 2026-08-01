using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Helpers;

public static class InterestExpenseHelper
{
    public static bool IsAutomaticInterest(FixedExpense expense)
    {
        var name = expense.Name ?? string.Empty;
        return name.Contains("descubierto", StringComparison.OrdinalIgnoreCase)
            && (name.Contains("interés", StringComparison.OrdinalIgnoreCase)
                || name.Contains("interes", StringComparison.OrdinalIgnoreCase));
    }
}
