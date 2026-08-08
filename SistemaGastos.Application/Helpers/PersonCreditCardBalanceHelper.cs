using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Helpers;

public static class PersonCreditCardBalanceHelper
{
    public static bool ShouldInclude(
        CreditCardTransaction transaction,
        DateTime targetMonth,
        DateTime? fullCollectionCutoff,
        IEnumerable<DateTime> individualCollectionDates)
    {
        var collectionDates = individualCollectionDates.ToList();
        var recurring = transaction.Fixed || (transaction.Installments ?? 1) > 1;

        if (!IsDueInMonth(transaction, targetMonth))
            return false;

        if (recurring)
        {
            // An individual payment settles only that month's installment or
            // recurring charge; it must not erase future months.
            if (collectionDates.Any(date => SameMonth(date, targetMonth)))
                return false;

            if (!fullCollectionCutoff.HasValue)
                return true;

            var recurringCutoffMonth = new DateTime(
                fullCollectionCutoff.Value.Year,
                fullCollectionCutoff.Value.Month,
                1);
            var recurringDueMonth = new DateTime(targetMonth.Year, targetMonth.Month, 1);

            // A full collection closes every installment/cargo already present
            // in that cycle. Future cycles remain payable, and a genuinely new
            // recurring item created after collection still belongs to the month.
            return recurringDueMonth > recurringCutoffMonth
                || transaction.TransactionDate > fullCollectionCutoff.Value;
        }

        if (collectionDates.Count > 0)
            return false;

        if (!fullCollectionCutoff.HasValue)
            return true;

        // A full collection settles the statement that was due at that time.
        // It must not discard a purchase from the next statement merely because
        // the purchase date precedes the day on which the previous cycle was collected.
        var cutoffMonth = new DateTime(
            fullCollectionCutoff.Value.Year,
            fullCollectionCutoff.Value.Month,
            1);
        var dueMonth = new DateTime(targetMonth.Year, targetMonth.Month, 1);
        return dueMonth > cutoffMonth || transaction.TransactionDate > fullCollectionCutoff.Value;
    }

    public static bool IsDueInMonth(CreditCardTransaction transaction, DateTime targetMonth)
    {
        if (transaction.Account == null) return false;

        var firstDueMonth = GetFirstDueMonth(transaction);
        var normalizedTargetMonth = new DateTime(targetMonth.Year, targetMonth.Month, 1);
        if (normalizedTargetMonth < firstDueMonth) return false;

        if (transaction.Fixed && (transaction.Installments ?? 1) <= 1)
            return true;

        var installments = transaction.Installments ?? 1;
        if (installments <= 1)
            return normalizedTargetMonth == firstDueMonth;

        // ActualInstallment represents the installment currently shown by the
        // card module, not the installment that existed on the purchase date.
        var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var installmentInTargetMonth = (transaction.ActualInstallment ?? 1)
            + MonthDiff(currentMonth, normalizedTargetMonth);

        return installmentInTargetMonth >= 1 && installmentInTargetMonth <= installments;
    }

    private static DateTime GetFirstDueMonth(CreditCardTransaction transaction)
    {
        var purchaseMonth = new DateTime(transaction.TransactionDate.Year, transaction.TransactionDate.Month, 1);
        var closesNextMonth = transaction.Account!.ClosingDay.HasValue
                           && transaction.TransactionDate.Day > transaction.Account.ClosingDay.Value;
        var statementMonth = purchaseMonth.AddMonths(closesNextMonth ? 1 : 0);
        return statementMonth.AddMonths(transaction.Account.DueMonthOffset ?? 1);
    }

    private static int MonthDiff(DateTime from, DateTime to) =>
        (to.Year - from.Year) * 12 + to.Month - from.Month;

    private static bool SameMonth(DateTime left, DateTime right) =>
        left.Year == right.Year && left.Month == right.Month;
}
