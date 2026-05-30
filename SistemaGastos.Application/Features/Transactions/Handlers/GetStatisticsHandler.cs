using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Transactions.Queries;
using SistemaGastos.Application.Interfaces;
using System.Globalization;

namespace SistemaGastos.Application.Features.Transactions.Handlers;

public class GetStatisticsHandler(IApplicationDbContext context)
    : IRequestHandler<GetStatisticsQuery, StatisticsDto>
{
    public async Task<StatisticsDto> Handle(GetStatisticsQuery request, CancellationToken cancellationToken)
    {
        var culture = new CultureInfo("es-AR");
        var now = DateTime.Now;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
        var startOfLastMonth = startOfMonth.AddMonths(-1);
        var endOfLastMonth = startOfMonth.AddDays(-1);

        var dto = new StatisticsDto();

        // ============================================
        // 1. SALDOS GENERALES
        // ============================================
        var accounts = await context.Account
            .Where(a => a.UserID == request.UserID)
            .ToListAsync(cancellationToken);

        dto.TotalBalance = accounts.Sum(a => a.Balance);
        dto.TotalBalanceFmt = dto.TotalBalance.ToString("C", culture);
        dto.TotalAccounts = accounts.Count;

        // Cuentas detalladas
        dto.Accounts = accounts.Select(a => new AccountBalanceDto
        {
            AccountName = a.Name,
            AccountType = a.Type.ToString(),
            Balance = a.Balance,
            BalanceFmt = a.Balance.ToString("C", culture),
            Percentage = dto.TotalBalance != 0 ? (a.Balance / dto.TotalBalance * 100) : 0
        }).OrderByDescending(a => a.Balance).ToList();

        // ============================================
        // 2. DEUDA DE TARJETAS DE CRÉDITO
        // ============================================
        var creditCards = await context.CreditCardTransaction
            .Include(c => c.Account)
            .Where(c => c.Account.UserID == request.UserID
                && c.ActualInstallment < c.Installments)
            .ToListAsync(cancellationToken);

        dto.ActiveInstallments = creditCards.Count;
        dto.TotalCreditCards = await context.Account
            .CountAsync(a => a.UserID == request.UserID && a.Type == Domain.Enums.AccountType.TarjetaCredito, cancellationToken);

        // Calcular deuda total pendiente
        dto.TotalPendingInstallments = (decimal)creditCards.Sum(c =>
            c.Amount * (c.Installments - c.ActualInstallment));
        dto.TotalPendingInstallmentsFmt = dto.TotalPendingInstallments.ToString("C", culture);

        dto.TotalCreditCardDebt = dto.TotalPendingInstallments;
        dto.TotalCreditCardDebtFmt = dto.TotalCreditCardDebt.ToString("C", culture);

        // Cuotas del próximo mes
        var nextMonth = now.AddMonths(1);
        dto.NextMonthInstallments = creditCards
            .Where(c =>
            {
                var monthsSincePurchase = (nextMonth.Year - c.TransactionDate.Year) * 12 + (nextMonth.Month - c.TransactionDate.Month);
                return monthsSincePurchase >= 0
                    && monthsSincePurchase < c.Installments
                    && monthsSincePurchase >= c.ActualInstallment;
            })
            .Sum(c => c.Amount);
        dto.NextMonthInstallmentsFmt = dto.NextMonthInstallments.ToString("C", culture);

        // Patrimonio neto
        dto.NetWorth = dto.TotalBalance - dto.TotalCreditCardDebt;
        dto.NetWorthFmt = dto.NetWorth.ToString("C", culture);

        // ============================================
        // CARGA DE TRANSACCIONES (últimos 6 meses, query única)
        // ============================================
        var sixMonthsAgo = startOfMonth.AddMonths(-5);
        var allTransactions = await context.Transaction
            .AsNoTracking()
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Where(t => t.Account.UserID == request.UserID && t.Date >= sixMonthsAgo)
            .ToListAsync(cancellationToken);

        // ============================================
        // 3. MES ACTUAL
        // ============================================
        var currentMonthTransactions = allTransactions
            .Where(t => t.Date >= startOfMonth && t.Date <= endOfMonth)
            .ToList();

        dto.CurrentMonthIncome = currentMonthTransactions
            .Where(t => t.Category.Type == "Ingreso")
            .Sum(t => t.Amount);
        dto.CurrentMonthIncomeFmt = dto.CurrentMonthIncome.ToString("C", culture);

        dto.CurrentMonthExpense = currentMonthTransactions
            .Where(t => t.Category.Type == "Gasto")
            .Sum(t => t.Amount);
        dto.CurrentMonthExpenseFmt = dto.CurrentMonthExpense.ToString("C", culture);

        dto.CurrentMonthBalance = dto.CurrentMonthIncome - dto.CurrentMonthExpense;
        dto.CurrentMonthBalanceFmt = dto.CurrentMonthBalance.ToString("C", culture);

        dto.CurrentMonthSavings = dto.CurrentMonthBalance;
        dto.CurrentMonthSavingsFmt = dto.CurrentMonthSavings.ToString("C", culture);

        dto.SavingsRate = dto.CurrentMonthIncome != 0
            ? (dto.CurrentMonthSavings / dto.CurrentMonthIncome * 100)
            : 0;

        // ============================================
        // 4. MES ANTERIOR
        // ============================================
        var lastMonthTransactions = allTransactions
            .Where(t => t.Date >= startOfLastMonth && t.Date <= endOfLastMonth)
            .ToList();

        dto.LastMonthIncome = lastMonthTransactions
            .Where(t => t.Category.Type == "Ingreso")
            .Sum(t => t.Amount);
        dto.LastMonthIncomeFmt = dto.LastMonthIncome.ToString("C", culture);

        dto.LastMonthExpense = lastMonthTransactions
            .Where(t => t.Category.Type == "Gasto")
            .Sum(t => t.Amount);
        dto.LastMonthExpenseFmt = dto.LastMonthExpense.ToString("C", culture);

        // Variaciones
        dto.IncomeVariation = dto.LastMonthIncome != 0
            ? ((dto.CurrentMonthIncome - dto.LastMonthIncome) / dto.LastMonthIncome * 100)
            : 0;

        dto.ExpenseVariation = dto.LastMonthExpense != 0
            ? ((dto.CurrentMonthExpense - dto.LastMonthExpense) / dto.LastMonthExpense * 100)
            : 0;

        // ============================================
        // 5. TOP CATEGORÍAS
        // ============================================
        var topExpenses = currentMonthTransactions
            .Where(t => t.Category.Type == "Gasto")
            .GroupBy(t => t.Category.Name)
            .Select(g => new CategoryStatDto
            {
                CategoryName = g.Key,
                Amount = g.Sum(t => t.Amount),
                AmountFmt = g.Sum(t => t.Amount).ToString("C", culture),
                Percentage = dto.CurrentMonthExpense != 0
                    ? (g.Sum(t => t.Amount) / dto.CurrentMonthExpense * 100)
                    : 0,
                TransactionCount = g.Count()
            })
            .OrderByDescending(c => c.Amount)
            .Take(5)
            .ToList();

        dto.TopExpenseCategories = topExpenses;

        var topIncomes = currentMonthTransactions
            .Where(t => t.Category.Type == "Ingreso")
            .GroupBy(t => t.Category.Name)
            .Select(g => new CategoryStatDto
            {
                CategoryName = g.Key,
                Amount = g.Sum(t => t.Amount),
                AmountFmt = g.Sum(t => t.Amount).ToString("C", culture),
                Percentage = dto.CurrentMonthIncome != 0
                    ? (g.Sum(t => t.Amount) / dto.CurrentMonthIncome * 100)
                    : 0,
                TransactionCount = g.Count()
            })
            .OrderByDescending(c => c.Amount)
            .Take(5)
            .ToList();

        dto.TopIncomeCategories = topIncomes;

        // ============================================
        // 6. PROYECCIONES
        // ============================================
        var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
        var daysPassed = now.Day;
        dto.DaysUntilMonthEnd = daysInMonth - daysPassed;

        dto.AverageDailyExpense = daysPassed > 0
            ? dto.CurrentMonthExpense / daysPassed
            : 0;
        dto.AverageDailyExpenseFmt = dto.AverageDailyExpense.ToString("C", culture);

        var estimatedRemainingExpense = dto.AverageDailyExpense * dto.DaysUntilMonthEnd;
        dto.EstimatedMonthEndBalance = dto.TotalBalance - estimatedRemainingExpense;
        dto.EstimatedMonthEndBalanceFmt = dto.EstimatedMonthEndBalance.ToString("C", culture);

        // Proyección próximo mes (simplificada)
        dto.ProjectedBalanceNextMonth = dto.TotalBalance + dto.CurrentMonthBalance - dto.NextMonthInstallments;
        dto.ProjectedBalanceNextMonthFmt = dto.ProjectedBalanceNextMonth.ToString("C", culture);

        // ============================================
        // 7. ÚLTIMAS TRANSACCIONES
        // ============================================
        dto.RecentTransactions = await context.Transaction
            .AsNoTracking()
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Where(t => t.Account.UserID == request.UserID)
            .OrderByDescending(t => t.Date)
            .Take(10)
            .Select(t => new TransactionDTO
            {
                ID = t.ID,
                Date = t.Date,
                DateFormatted = t.Date.ToString("dd/MM/yyyy"),
                Amount = t.Amount,
                AmountFormatted = t.Amount.ToString("C", culture),
                Description = t.Description ?? string.Empty,
                AccountName = t.Account.Name,
                AccountCurrency = t.Account.Currency ?? "ARS",
                CategoryName = t.Category.Name,
                IsIncome = t.Category.Type == "Ingreso"
            })
            .ToListAsync(cancellationToken);

        // ============================================
        // 8. TENDENCIA HISTÓRICA (últimos 6 meses)
        // ============================================
        dto.MonthlyTrend = Enumerable.Range(0, 6)
            .Select(i =>
            {
                var mStart = sixMonthsAgo.AddMonths(i);
                var mEnd = mStart.AddMonths(1);
                var txs = allTransactions
                    .Where(t => t.Date >= mStart && t.Date < mEnd)
                    .ToList();
                var inc = txs.Where(t => t.Category.Type == "Ingreso").Sum(t => t.Amount);
                var exp = txs.Where(t => t.Category.Type == "Gasto").Sum(t => t.Amount);
                return new MonthlyTrendDto
                {
                    Month = mStart.ToString("MMM yy", culture).Replace(".", ""),
                    Income = inc,
                    Expense = exp,
                    Balance = inc - exp
                };
            })
            .ToList();

        return dto;
    }
}
