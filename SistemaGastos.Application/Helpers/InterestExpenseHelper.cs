using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Helpers;

public static class InterestExpenseHelper
{
    public static bool IsAutomaticInterest(FixedExpense expense)
    {
        var name = expense.Name ?? string.Empty;
        return name.StartsWith("Intereses por descubierto", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("Cobro de interés por descubierto", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("Cobro de interes por descubierto", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAutomaticOverdraftCharge(FixedExpense expense)
    {
        var name = expense.Name ?? string.Empty;
        return IsAutomaticInterest(expense)
            || name.StartsWith("IVA sobre intereses por descubierto", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("IVA ", StringComparison.OrdinalIgnoreCase)
               && (name.Contains("interés por descubierto", StringComparison.OrdinalIgnoreCase)
                   || name.Contains("interes por descubierto", StringComparison.OrdinalIgnoreCase))
            || name.StartsWith("Impuesto de sellos por descubierto", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsLiveAccrual(FixedExpense expense, DateTime today)
    {
        if (!IsAutomaticOverdraftCharge(expense) || string.IsNullOrWhiteSpace(expense.PaymentYearMonth))
            return false;

        var nextMonth = new DateTime(today.Year, today.Month, 1).AddMonths(1);
        return expense.PaymentYearMonth == $"{nextMonth.Year}-{nextMonth.Month:D2}";
    }
}
