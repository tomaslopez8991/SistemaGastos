namespace SistemaGastos.Application.DTOs;

public class StatisticsDto
{
    // Saldos generales
    public decimal TotalBalance { get; set; }
    public string TotalBalanceFmt { get; set; } = string.Empty;
    public decimal TotalCreditCardDebt { get; set; }
    public string TotalCreditCardDebtFmt { get; set; } = string.Empty;
    public decimal NetWorth { get; set; }
    public string NetWorthFmt { get; set; } = string.Empty;

    // Mes actual
    public decimal CurrentMonthIncome { get; set; }
    public string CurrentMonthIncomeFmt { get; set; } = string.Empty;
    public decimal CurrentMonthExpense { get; set; }
    public string CurrentMonthExpenseFmt { get; set; } = string.Empty;
    public decimal CurrentMonthBalance { get; set; }
    public string CurrentMonthBalanceFmt { get; set; } = string.Empty;
    public decimal CurrentMonthSavings { get; set; }
    public string CurrentMonthSavingsFmt { get; set; } = string.Empty;
    public decimal SavingsRate { get; set; } // Porcentaje de ahorro

    // Comparación con mes anterior
    public decimal LastMonthIncome { get; set; }
    public string LastMonthIncomeFmt { get; set; } = string.Empty;
    public decimal LastMonthExpense { get; set; }
    public string LastMonthExpenseFmt { get; set; } = string.Empty;
    public decimal IncomeVariation { get; set; } // % cambio
    public decimal ExpenseVariation { get; set; } // % cambio

    // Top categorías del mes
    public List<CategoryStatDto> TopExpenseCategories { get; set; } = new();
    public List<CategoryStatDto> TopIncomeCategories { get; set; } = new();

    // Tarjetas de crédito
    public int TotalCreditCards { get; set; }
    public int ActiveInstallments { get; set; }
    public decimal TotalPendingInstallments { get; set; }
    public string TotalPendingInstallmentsFmt { get; set; } = string.Empty;
    public decimal NextMonthInstallments { get; set; }
    public string NextMonthInstallmentsFmt { get; set; } = string.Empty;

    // Cuentas
    public int TotalAccounts { get; set; }
    public List<AccountBalanceDto> Accounts { get; set; } = new();

    // Proyecciones
    public decimal ProjectedBalanceNextMonth { get; set; }
    public string ProjectedBalanceNextMonthFmt { get; set; } = string.Empty;
    public decimal AverageDailyExpense { get; set; }
    public string AverageDailyExpenseFmt { get; set; } = string.Empty;
    public int DaysUntilMonthEnd { get; set; }
    public decimal EstimatedMonthEndBalance { get; set; }
    public string EstimatedMonthEndBalanceFmt { get; set; } = string.Empty;

    // Últimas transacciones
    public List<TransactionDTO> RecentTransactions { get; set; } = new();

    // Tendencia histórica (últimos 6 meses)
    public List<MonthlyTrendDto> MonthlyTrend { get; set; } = new();
}

public class CategoryStatDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AmountFmt { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public int TransactionCount { get; set; }
}

public class AccountBalanceDto
{
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string BalanceFmt { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
}

public class MonthlyTrendDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Balance { get; set; }
}
