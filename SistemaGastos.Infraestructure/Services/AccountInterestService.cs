using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Application.Helpers;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Infraestructure.Services;

public class AccountInterestService(IApplicationDbContext context) : IAccountInterestService
{
    private const int InterestCategoryID = 8;

    public async Task RunAccrualAsync(CancellationToken cancellationToken = default)
    {
        var settings = await context.AccountInterestSetting
            .Where(s => s.Enabled)
            .ToListAsync(cancellationToken);

        var yesterday = DateTime.Today.AddDays(-1);

        foreach (var setting in settings)
        {
            var lastLog = await context.AccountInterestDailyLog
                .Where(l => l.AccountID == setting.AccountID)
                .OrderByDescending(l => l.Date)
                .FirstOrDefaultAsync(cancellationToken);

            var fromDate = lastLog != null ? lastLog.Date.AddDays(1) : setting.CreatedAt.Date;

            if (fromDate <= yesterday)
            {
                await BuildAndSaveLogsAsync(setting, fromDate, yesterday, lastLog?.Balance, lastLog?.DayCounter ?? 0, lastLog?.CumulativeInterest ?? 0m, cancellationToken);
            }

            await CreateMonthlyChargesIfDueAsync(setting, cancellationToken);
            await SyncCurrentMonthExpenseAsync(setting, cancellationToken);
        }
    }

    public async Task RecalculateAsync(int userId, CancellationToken cancellationToken = default)
    {
        var settings = await context.AccountInterestSetting
            .Where(s => s.Enabled && s.UserID == userId)
            .ToListAsync(cancellationToken);

        var yesterday = DateTime.Today.AddDays(-1);
        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        foreach (var setting in settings)
        {
            // Recalcula únicamente el mes en curso (la racha de saldo negativo
            // se asume iniciada este mes), reconstruyendo el saldo día por día
            // a partir de las transacciones. Los días con saldo >= 0 quedan
            // con interés $0 y no se contabilizan.
            var logsToRemove = await context.AccountInterestDailyLog
                .Where(l => l.AccountID == setting.AccountID && l.Date >= monthStart)
                .ToListAsync(cancellationToken);

            context.AccountInterestDailyLog.RemoveRange(logsToRemove);
            await context.SaveChangesAsync(cancellationToken);

            var previousLog = await context.AccountInterestDailyLog
                .Where(l => l.AccountID == setting.AccountID && l.Date < monthStart)
                .OrderByDescending(l => l.Date)
                .FirstOrDefaultAsync(cancellationToken);

            if (monthStart <= yesterday)
            {
                await BuildAndSaveLogsAsync(setting, monthStart, yesterday, previousLog?.Balance, 0, 0m, cancellationToken);
            }
        }
    }

    public async Task<decimal> GetTotalAccruedInterestAsync(int userId, CancellationToken cancellationToken = default)
    {
        var accountIds = await context.AccountInterestSetting
            .Where(s => s.Enabled && s.UserID == userId)
            .Select(s => s.AccountID)
            .ToListAsync(cancellationToken);

        if (accountIds.Count == 0) return 0m;

        decimal total = 0m;

        foreach (var accountId in accountIds)
        {
            var lastLog = await context.AccountInterestDailyLog
                .Where(l => l.AccountID == accountId)
                .OrderByDescending(l => l.Date)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastLog != null) total += lastLog.CumulativeInterest;
        }

        return total;
    }

    /// <summary>
    /// Reconstruye el saldo diario de la cuenta entre fromDate y toDate (inclusive)
    /// partiendo del saldo actual y revirtiendo los movimientos posteriores a cada día.
    /// </summary>
    private async Task BuildAndSaveLogsAsync(
        AccountInterestSetting setting,
        DateTime fromDate,
        DateTime toDate,
        decimal? previousBalance,
        int previousDayCounter,
        decimal previousCumulativeInterest,
        CancellationToken cancellationToken)
    {
        var account = await context.Account
            .FirstOrDefaultAsync(a => a.ID == setting.AccountID, cancellationToken);

        if (account == null) return;

        var movements = await context.Transaction
            .Where(t => t.AccountID == setting.AccountID && t.Date.Date > fromDate.AddDays(-1) && t.Date.Date <= toDate)
            .Select(t => new { t.Date, t.Amount, CategoryType = t.Category!.Type })
            .ToListAsync(cancellationToken);

        var deltasByDay = movements
            .GroupBy(m => m.Date.Date)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(m => m.CategoryType == "Ingreso" ? m.Amount : -m.Amount));

        // Saldo al cierre de cada día, reconstruido hacia atrás desde el saldo actual.
        var balances = new Dictionary<DateTime, decimal> { [toDate] = account.Balance };
        for (var d = toDate; d > fromDate; d = d.AddDays(-1))
        {
            deltasByDay.TryGetValue(d, out var delta);
            balances[d.AddDays(-1)] = balances[d] - delta;
        }

        var newLogs = new List<AccountInterestDailyLog>();
        var prevBalance = previousBalance;
        var dayCounter = previousDayCounter;
        var cumulativeInterest = previousCumulativeInterest;

        for (var d = fromDate; d <= toDate; d = d.AddDays(1))
        {
            if (d.Day == 1)
            {
                dayCounter = 0;
                cumulativeInterest = 0m;
                prevBalance = null;
            }

            var balance = Math.Round(balances[d], 2);

            dayCounter = (prevBalance.HasValue && balance == prevBalance.Value) ? dayCounter + 1 : 1;
            prevBalance = balance;

            // (|saldo| * tasa * días consecutivos con este saldo) / 365
            var dailyInterest = balance < 0
                ? Math.Round(Math.Abs(balance) * setting.InterestRate * dayCounter / 365m, 2)
                : 0m;

            cumulativeInterest += dailyInterest;

            newLogs.Add(new AccountInterestDailyLog
            {
                AccountID = setting.AccountID,
                UserID = setting.UserID,
                Date = d,
                Balance = balance,
                DayCounter = dayCounter,
                DailyInterest = dailyInterest,
                CumulativeInterest = cumulativeInterest,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.AccountInterestDailyLog.AddRangeAsync(newLogs, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task CreateMonthlyChargesIfDueAsync(AccountInterestSetting setting, CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        if (today.Day < 7) return;

        var monthCursor = new DateTime(setting.CreatedAt.Year, setting.CreatedAt.Month, 1);
        var currentMonthStart = new DateTime(today.Year, today.Month, 1);

        while (monthCursor < currentMonthStart)
        {
            var year = monthCursor.Year;
            var month = monthCursor.Month;

            var monthlyCharge = await context.AccountInterestMonthlyCharge
                .FirstOrDefaultAsync(c => c.AccountID == setting.AccountID && c.Year == year && c.Month == month, cancellationToken);

            var totalInterest = monthlyCharge?.TotalInterest
                ?? await context.AccountInterestDailyLog
                    .Where(l => l.AccountID == setting.AccountID && l.Date.Year == year && l.Date.Month == month)
                    .SumAsync(l => l.DailyInterest, cancellationToken);

            totalInterest = Math.Round(totalInterest, 2);

            if (totalInterest > 0)
            {
                var account = await context.Account
                    .FirstOrDefaultAsync(a => a.ID == setting.AccountID, cancellationToken);

                var categoryExists = await context.Category
                    .AnyAsync(c => c.ID == InterestCategoryID, cancellationToken);

                if (account != null && categoryExists)
                {
                    var chargeMonth = monthCursor.AddMonths(1);
                    var chargeMonthKey = $"{chargeMonth.Year}-{chargeMonth.Month:D2}";
                    var expenseName = $"Intereses por descubierto - {account.Name} ({monthCursor:MM/yyyy})";
                    var fixedExpense = await context.FixedExpense
                        .FirstOrDefaultAsync(f => f.UserID == setting.UserID
                                               && f.AccountID == account.ID
                                               && f.CategoryID == InterestCategoryID
                                               && f.PaymentYearMonth == chargeMonthKey
                                               && f.Name == expenseName,
                            cancellationToken);

                    if (fixedExpense == null)
                    {
                        fixedExpense = new FixedExpense
                        {
                            UserID = setting.UserID,
                            AccountID = account.ID,
                            CategoryID = InterestCategoryID,
                            Name = expenseName,
                            Amount = totalInterest,
                            Currency = account.Currency,
                            PaymentDay = 7,
                            Active = true,
                            StartDate = chargeMonth,
                            PaymentYearMonth = chargeMonthKey
                        };
                        await context.FixedExpense.AddAsync(fixedExpense, cancellationToken);
                        await context.SaveChangesAsync(cancellationToken);
                    }
                    else
                    {
                        fixedExpense.Active = true;
                    }

                    if (monthlyCharge?.TransactionID is int transactionId)
                    {
                        var previousTransaction = await context.Transaction
                            .FirstOrDefaultAsync(t => t.ID == transactionId && t.FixedExpenseID == null, cancellationToken);
                        if (previousTransaction != null)
                            previousTransaction.FixedExpenseID = fixedExpense.ID;
                    }
                }
            }

            if (monthlyCharge == null)
            {
                await context.AccountInterestMonthlyCharge.AddAsync(new AccountInterestMonthlyCharge
                {
                    AccountID = setting.AccountID,
                    UserID = setting.UserID,
                    Year = year,
                    Month = month,
                    TotalInterest = totalInterest,
                    TransactionID = null,
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);

            monthCursor = monthCursor.AddMonths(1);
        }
    }

    private async Task SyncCurrentMonthExpenseAsync(AccountInterestSetting setting, CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var dueMonth = monthStart.AddMonths(1);
        var dueMonthKey = $"{dueMonth.Year}-{dueMonth.Month:D2}";
        var accruedInterest = await context.AccountInterestDailyLog
            .Where(l => l.AccountID == setting.AccountID
                     && l.Date.Year == today.Year
                     && l.Date.Month == today.Month)
            .OrderByDescending(l => l.Date)
            .Select(l => l.CumulativeInterest)
            .FirstOrDefaultAsync(cancellationToken);

        var account = await context.Account
            .FirstOrDefaultAsync(a => a.ID == setting.AccountID, cancellationToken);
        if (account == null) return;
        var expenseName = $"Intereses por descubierto - {account.Name} ({monthStart:MM/yyyy})";

        var candidates = await context.FixedExpense
            .Where(f => f.UserID == setting.UserID && f.AccountID == setting.AccountID)
            .ToListAsync(cancellationToken);
        var interestExpenses = candidates.Where(InterestExpenseHelper.IsAutomaticInterest).ToList();
        var currentExpense = interestExpenses
            .FirstOrDefault(f => f.PaymentYearMonth == dueMonthKey && f.Name == expenseName);

        if (currentExpense == null)
        {
            currentExpense = new FixedExpense
            {
                UserID = setting.UserID,
                AccountID = account.ID,
                Name = expenseName
            };
            await context.FixedExpense.AddAsync(currentExpense, cancellationToken);
        }

        currentExpense.Amount = Math.Round(accruedInterest, 2);
        currentExpense.Currency = account.Currency;
        currentExpense.CategoryID = InterestCategoryID;
        currentExpense.PaymentDay = 7;
        currentExpense.Active = true;
        currentExpense.StartDate = dueMonth;
        currentExpense.PaymentYearMonth = dueMonthKey;

        foreach (var duplicate in interestExpenses.Where(f => f.ID != currentExpense.ID && f.PaymentYearMonth == dueMonthKey))
            duplicate.Active = false;

        foreach (var legacy in interestExpenses.Where(f => !f.Name.StartsWith("Intereses por descubierto -", StringComparison.OrdinalIgnoreCase)))
            legacy.Active = false;

        await context.SaveChangesAsync(cancellationToken);
    }
}
