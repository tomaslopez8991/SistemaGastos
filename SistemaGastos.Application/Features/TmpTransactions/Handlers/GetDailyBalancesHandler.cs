using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.TmpTransactions.Queries;
using SistemaGastos.Application.Helpers;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Enums;
using System.Globalization;

namespace SistemaGastos.Application.Features.TmpTransactions.Handlers;

public class GetDailyBalancesHandler(IApplicationDbContext context, IDolarService dolarService)
    : IRequestHandler<GetDailyBalancesQuery, DailyCalendarDto>
{
    /// <summary>Día de vencimiento a usar cuando la cuenta de TC no tiene DueDay configurado.</summary>
    private const int FallbackDueDay = 10;

    public async Task<DailyCalendarDto> Handle(GetDailyBalancesQuery request, CancellationToken cancellationToken)
    {
        var fechaActual = DateTime.Now;
        var culture = new CultureInfo("es-AR");
        decimal cotizacionDolar = await dolarService.GetDolarBolsaAsync();

        // ====================================================================
        // 1. SALDO INICIAL (igual que GetProjectedBalancesHandler)
        // ====================================================================
        var accounts = await context.Account
            .Where(a => a.UserID == request.UserID)
            .ToListAsync(cancellationToken);

        var saldoLiquidezARS = accounts
            .Where(a => a.Type != AccountType.TarjetaCredito && a.Currency == "ARS")
            .Sum(a => a.Balance);

        var saldoLiquidezUSD = accounts
            .Where(a => a.Type != AccountType.TarjetaCredito && (a.Currency == "USD" || a.Currency == "USDT"))
            .Sum(a => a.Balance);

        decimal saldoInicialTotal = saldoLiquidezARS + (saldoLiquidezUSD * cotizacionDolar);

        var ccAccounts = accounts.Where(a => a.Type == AccountType.TarjetaCredito).ToList();

        // ====================================================================
        // 2. DATOS BASE (proyecciones manuales, tarjetas, fijos)
        // ====================================================================
        var manualProjections = await context.TmpTransaction
            .Include(t => t.Category)
            .Where(t => t.UserID == request.UserID && t.DateTransaction.HasValue)
            .ToListAsync(cancellationToken);

        var cardTransactions = await context.CreditCardTransaction
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.Person)
            .Where(t => t.Account.UserID == request.UserID)
            .ToListAsync(cancellationToken);

        var allFixedExpenses = await context.FixedExpense
            .AsNoTracking()
            .Include(f => f.Category)
            .Include(f => f.Person)
            .Where(f => f.UserID == request.UserID && f.Active)
            .ToListAsync(cancellationToken);

        var paidFixedExpenses = await context.Transaction
            .AsNoTracking()
            .Where(t => t.Account.UserID == request.UserID && t.FixedExpenseID != null)
            .Select(t => new { ExpenseID = t.FixedExpenseID, Year = t.Date.Year, Month = t.Date.Month })
            .ToListAsync(cancellationToken);

        var allFixedIncomes = await context.FixedIncome
            .AsNoTracking()
            .Where(f => f.UserID == request.UserID && f.Active)
            .ToListAsync(cancellationToken);

        var receivedFixedIncomes = await context.Transaction
            .AsNoTracking()
            .Where(t => t.Account.UserID == request.UserID && t.FixedIncomeID != null)
            .Select(t => new { IncomeID = t.FixedIncomeID, Year = t.Date.Year, Month = t.Date.Month })
            .ToListAsync(cancellationToken);

        // ====================================================================
        // 3. RECORRER MESES HASTA EL MES SOLICITADO, ACUMULANDO SALDO
        // ====================================================================
        var startDate = new DateTime(fechaActual.Year, fechaActual.Month, 1);
        var requestedDate = new DateTime(request.Year, request.Month, 1);
        var monthIndex = Math.Max(0, MonthDiff(startDate, requestedDate));

        var mesProximo = startDate.AddMonths(1);
        var mesDespuesDelPago = startDate.AddMonths(2);

        var result = new DailyCalendarDto
        {
            Year = requestedDate.Year,
            Month = requestedDate.Month,
            MonthLabel = requestedDate.ToString("MMMM yyyy", culture),
            DolarRate = cotizacionDolar
        };

        decimal acumulado = saldoInicialTotal;

        for (int i = 0; i <= monthIndex; i++)
        {
            var m = startDate.AddMonths(i);
            var daysInMonth = DateTime.DaysInMonth(m.Year, m.Month);
            var itemsByDay = new Dictionary<int, List<DailyBalanceItemDto>>();

            void AddItem(int day, string description, decimal amount, bool isIncome, string sourceType)
            {
                if (amount <= 0) return;

                day = Math.Clamp(day, 1, daysInMonth);
                if (!itemsByDay.TryGetValue(day, out var list))
                {
                    list = new List<DailyBalanceItemDto>();
                    itemsByDay[day] = list;
                }

                list.Add(new DailyBalanceItemDto
                {
                    Description = description,
                    Amount = amount,
                    AmountFmt = amount.ToString("C", culture),
                    IsIncome = isIncome,
                    SourceType = sourceType
                });
            }

            // ────────────────────────────────────────────────────────────
            // A. PROYECCIONES MANUALES DEL MES
            // ────────────────────────────────────────────────────────────
            var monthTrans = manualProjections
                .Where(t => t.DateTransaction!.Value.Year == m.Year && t.DateTransaction.Value.Month == m.Month)
                .ToList();

            foreach (var t in monthTrans)
            {
                decimal amountArs = t.Currency == "USD" ? t.Amount * cotizacionDolar : t.Amount;
                bool isIncome = t.Category.Type == "Ingreso";

                var excludedDays = DistributionHelper.ParseExcludedDays(t.ExcludedDays);
                var dist = DistributionHelper.Distribute(amountArs, t.DateTransaction!.Value.Day, t.DistributionEndDay, excludedDays, daysInMonth)
                    .OrderBy(kv => kv.Key).ToList();

                foreach (var (kv, idx) in dist.Select((kv, idx) => (kv, idx)))
                {
                    var desc = dist.Count > 1 ? $"{t.Description} (día {idx + 1}/{dist.Count})" : t.Description;
                    AddItem(kv.Key, desc, kv.Value, isIncome, "Planificado");
                }
            }

            bool hasCreditCardPayment = monthTrans.Any(t =>
                t.Category.Type == "Gasto"
                && !string.IsNullOrEmpty(t.Description)
                && t.Description.Contains("Total TC", StringComparison.OrdinalIgnoreCase));

            // ────────────────────────────────────────────────────────────
            // B. GASTOS FIJOS DEL MES
            // ────────────────────────────────────────────────────────────
            var paidThisMonthIds = paidFixedExpenses
                .Where(p => p.Year == m.Year && p.Month == m.Month)
                .Select(p => p.ExpenseID)
                .ToList();

            var fixedExpensesOfMonth = allFixedExpenses
                .Where(f => f.PaymentDay > 0
                         && f.PaymentDay <= daysInMonth
                         && !paidThisMonthIds.Contains(f.ID)
                         && (f.StartDate == null || new DateTime(f.StartDate.Value.Year, f.StartDate.Value.Month, 1) <= m))
                .ToList();

            foreach (var fe in fixedExpensesOfMonth)
            {
                decimal amountArs = fe.Currency == "USD" ? fe.Amount * cotizacionDolar : fe.Amount;

                var excludedDays = DistributionHelper.ParseExcludedDays(fe.ExcludedDays);
                var dist = DistributionHelper.Distribute(amountArs, fe.PaymentDay, fe.DistributionEndDay, excludedDays, daysInMonth)
                    .OrderBy(kv => kv.Key).ToList();

                foreach (var (kv, idx) in dist.Select((kv, idx) => (kv, idx)))
                {
                    var desc = dist.Count > 1 ? $"{fe.Name} (día {idx + 1}/{dist.Count})" : fe.Name;
                    AddItem(kv.Key, desc, kv.Value, false, "GastoFijo");
                }

                if (fe.PersonID != null)
                {
                    var pct = fe.PersonPercentage ?? 100m;
                    AddItem(fe.PaymentDay, $"{fe.Person?.Name ?? "Persona"} - {fe.Name}", amountArs * pct / 100m, true, "Personas");
                }
            }

            // ────────────────────────────────────────────────────────────
            // B2. INGRESOS FIJOS DEL MES
            // ────────────────────────────────────────────────────────────
            var receivedThisMonthIds = receivedFixedIncomes
                .Where(p => p.Year == m.Year && p.Month == m.Month)
                .Select(p => p.IncomeID)
                .ToList();

            var fixedIncomesOfMonth = allFixedIncomes
                .Where(f => f.ReceiptDay > 0
                         && f.ReceiptDay <= daysInMonth
                         && !receivedThisMonthIds.Contains(f.ID)
                         && (f.StartDate == null || new DateTime(f.StartDate.Value.Year, f.StartDate.Value.Month, 1) <= m))
                .ToList();

            foreach (var fi in fixedIncomesOfMonth)
            {
                decimal amountArs = fi.Currency == "USD" ? fi.Amount * cotizacionDolar : fi.Amount;

                var excludedDays = DistributionHelper.ParseExcludedDays(fi.ExcludedDays);
                var dist = DistributionHelper.Distribute(amountArs, fi.ReceiptDay, fi.DistributionEndDay, excludedDays, daysInMonth)
                    .OrderBy(kv => kv.Key).ToList();

                foreach (var (kv, idx) in dist.Select((kv, idx) => (kv, idx)))
                {
                    var desc = dist.Count > 1 ? $"{fi.Name} (día {idx + 1}/{dist.Count})" : fi.Name;
                    AddItem(kv.Key, desc, kv.Value, true, "IngresoFijo");
                }
            }

            // ────────────────────────────────────────────────────────────
            // C. TOTAL TC PRÓXIMO (mes inmediato siguiente al actual)
            // ────────────────────────────────────────────────────────────
            if (!hasCreditCardPayment && SameMonth(m, mesProximo))
            {
                foreach (var cc in ccAccounts)
                {
                    var deuda = Math.Abs(cc.Balance);
                    if (deuda <= 0) continue;

                    decimal amountArs = cc.Currency == "USD" ? deuda * cotizacionDolar : deuda;
                    AddItem(cc.DueDay ?? FallbackDueDay, $"Total TC - {cc.Name}", amountArs, false, "TarjetaCredito");
                }
            }

            // ────────────────────────────────────────────────────────────
            // D. TOTAL TC A FUTURO (cuotas y cargos agrupados por tarjeta)
            // ────────────────────────────────────────────────────────────
            if (!hasCreditCardPayment && m >= mesDespuesDelPago)
            {
                var totalesPorCuenta = new Dictionary<int, decimal>();

                foreach (var cardTx in cardTransactions)
                {
                    var compraMes = new DateTime(cardTx.TransactionDate.Year, cardTx.TransactionDate.Month, 1);

                    // Si la compra se hizo después del día de cierre, entra en el resumen siguiente
                    var closingDay = cardTx.Account.ClosingDay;
                    var mesesAlCierre = (closingDay.HasValue && cardTx.TransactionDate.Day > closingDay.Value) ? 1 : 0;
                    var mesResumen = compraMes.AddMonths(mesesAlCierre);
                    var vencimiento = mesResumen.AddMonths(cardTx.Account.DueMonthOffset ?? 1);

                    decimal montoArs = cardTx.Account.Currency == "USD" ? cardTx.Amount * cotizacionDolar : cardTx.Amount;
                    var dueDay = cardTx.Account.DueDay ?? FallbackDueDay;

                    bool esFijo = cardTx.Fixed;
                    bool esCuotas = cardTx.Installments > 1;
                    bool esVariable = !esFijo && !esCuotas;

                    decimal? montoDelMes = null;
                    string descripcion = cardTx.Description;

                    if (esVariable)
                    {
                        if (SameMonth(m, vencimiento)) montoDelMes = montoArs;
                    }
                    else if (esFijo && !esCuotas)
                    {
                        montoDelMes = montoArs;
                    }
                    else if (esCuotas)
                    {
                        int totalCuotas = (int)cardTx.Installments!;
                        int cuotaBase = (int)(cardTx.ActualInstallment ?? 0);
                        int offset = MonthDiff(vencimiento, m);
                        if (offset >= 0)
                        {
                            int cuotaEnMes = cuotaBase + offset;
                            if (cuotaEnMes <= totalCuotas)
                            {
                                montoDelMes = montoArs / totalCuotas;
                                descripcion = $"{cardTx.Description} (cuota {cuotaEnMes}/{totalCuotas})";
                            }
                        }
                    }

                    if (montoDelMes is not decimal monto || monto <= 0) continue;

                    totalesPorCuenta.TryGetValue(cardTx.AccountID, out var acumuladoCuenta);
                    totalesPorCuenta[cardTx.AccountID] = acumuladoCuenta + monto;

                    if (cardTx.PersonID != null)
                    {
                        var pct = cardTx.PersonPercentage ?? 100m;
                        AddItem(dueDay, $"{cardTx.Person?.Name ?? "Persona"} - {descripcion}", monto * pct / 100m, true, "Personas");
                    }
                }

                foreach (var (accountId, total) in totalesPorCuenta)
                {
                    var cc = ccAccounts.FirstOrDefault(a => a.ID == accountId);
                    if (cc == null) continue;

                    AddItem(cc.DueDay ?? FallbackDueDay, $"Total TC - {cc.Name}", total, false, "TarjetaCredito");
                }
            }

            // ────────────────────────────────────────────────────────────
            // TOTAL DEL MES Y ACUMULADO
            // ────────────────────────────────────────────────────────────
            decimal netoDelMes = itemsByDay.Values
                .SelectMany(items => items)
                .Sum(item => item.IsIncome ? item.Amount : -item.Amount);

            if (i == monthIndex)
            {
                result.StartingBalance = acumulado;
                result.StartingBalanceFmt = acumulado.ToString("C", culture);

                decimal running = acumulado;
                for (int day = 1; day <= daysInMonth; day++)
                {
                    var items = itemsByDay.TryGetValue(day, out var list)
                        ? list.OrderBy(x => x.IsIncome ? 0 : 1).ToList()
                        : new List<DailyBalanceItemDto>();

                    decimal income = items.Where(x => x.IsIncome).Sum(x => x.Amount);
                    decimal expense = items.Where(x => !x.IsIncome).Sum(x => x.Amount);
                    running += income - expense;

                    result.Days.Add(new DailyBalanceDto
                    {
                        Day = day,
                        Date = new DateTime(m.Year, m.Month, day).ToString("yyyy-MM-dd"),
                        Income = income,
                        Expense = expense,
                        Balance = running,
                        BalanceFmt = running.ToString("C", culture),
                        Items = items
                    });
                }
            }

            acumulado += netoDelMes;
        }

        return result;
    }

    private bool SameMonth(DateTime a, DateTime b) => a.Year == b.Year && a.Month == b.Month;
    private int MonthDiff(DateTime from, DateTime to) => (to.Year - from.Year) * 12 + (to.Month - from.Month);
}
