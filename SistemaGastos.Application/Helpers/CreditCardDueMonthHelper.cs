using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Helpers;

public static class CreditCardDueMonthHelper
{
    public static bool IsFirstPaymentDueInMonth(CreditCardTransaction transaction, DateTime targetMonth)
    {
        if (transaction.Account == null) return false;

        var purchaseMonth = new DateTime(transaction.TransactionDate.Year, transaction.TransactionDate.Month, 1);
        var closesNextMonth = transaction.Account.ClosingDay.HasValue
                           && transaction.TransactionDate.Day > transaction.Account.ClosingDay.Value;
        var statementMonth = purchaseMonth.AddMonths(closesNextMonth ? 1 : 0);
        var dueMonth = statementMonth.AddMonths(transaction.Account.DueMonthOffset ?? 1);

        return dueMonth.Year == targetMonth.Year && dueMonth.Month == targetMonth.Month;
    }
}
