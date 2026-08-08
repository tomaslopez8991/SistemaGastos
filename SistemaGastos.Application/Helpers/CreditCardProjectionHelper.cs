using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Helpers;

public static class CreditCardProjectionHelper
{
    public static decimal GetAmountDueArs(CreditCardTransaction transaction, DateTime targetMonth, DateTime currentMonth, decimal dolarRate)
    {
        if (transaction.Account is null) return 0m;

        var target = MonthStart(targetMonth);
        var current = MonthStart(currentMonth);
        var firstDue = GetFirstDueMonth(transaction);
        if (target < firstDue) return 0m;

        var installments = transaction.Installments ?? 1;
        if (installments > 1)
        {
            var installment = (transaction.ActualInstallment ?? 1) + MonthDiff(current, target);
            if (installment < 1 || installment > installments) return 0m;
        }
        else if (!transaction.Fixed && target != firstDue)
        {
            return 0m;
        }

        return transaction.Account.Currency == "USD" ? transaction.Amount * dolarRate : transaction.Amount;
    }

    private static DateTime GetFirstDueMonth(CreditCardTransaction transaction)
    {
        var purchaseMonth = MonthStart(transaction.TransactionDate);
        var afterClosing = transaction.Account!.ClosingDay.HasValue
            && transaction.TransactionDate.Day > transaction.Account.ClosingDay.Value;
        return purchaseMonth.AddMonths(afterClosing ? 1 : 0)
            .AddMonths(transaction.Account.DueMonthOffset ?? 1);
    }

    private static DateTime MonthStart(DateTime date) => new(date.Year, date.Month, 1);
    private static int MonthDiff(DateTime from, DateTime to) => (to.Year - from.Year) * 12 + to.Month - from.Month;
}
