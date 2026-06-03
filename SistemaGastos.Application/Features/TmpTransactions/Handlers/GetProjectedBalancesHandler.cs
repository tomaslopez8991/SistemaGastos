using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.TmpTransactions.Queries;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Enums;
using System.Globalization;

namespace SistemaGastos.Application.Features.TmpTransactions.Handlers;

public class GetProjectedBalancesHandler(IApplicationDbContext context, IDolarService dolarService)
    : IRequestHandler<GetProjectedBalancesQuery, List<MonthlyBalanceDto>>
{
    public async Task<List<MonthlyBalanceDto>> Handle(GetProjectedBalancesQuery request, CancellationToken cancellationToken)
    {
        var fechaActual = DateTime.Now;
        decimal cotizacionDolar = await dolarService.GetDolarBolsaAsync();

        // ====================================================================
        // 1. CALCULAR SALDO INICIAL
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

        // ====================================================================
        // 2. DEUDA DE TARJETAS (resumen próximo a pagar)
        // ====================================================================
        var deudaTarjetasArs = accounts
            .Where(a => a.Type == AccountType.TarjetaCredito && a.Currency == "ARS")
            .Sum(a => a.Balance);

        var deudaTarjetasUsd = accounts
            .Where(a => a.Type == AccountType.TarjetaCredito && a.Currency == "USD")
            .Sum(a => a.Balance);

        decimal deudaResumenProximo = Math.Abs(deudaTarjetasArs) + (Math.Abs(deudaTarjetasUsd) * cotizacionDolar);

        // ====================================================================
        // 3. OBTENER PROYECCIONES MANUALES
        // ====================================================================
        var manualProjections = await context.TmpTransaction
            .Include(t => t.Category)
            .Where(t => t.UserID == request.UserID && t.DateTransaction.HasValue)
            .ToListAsync(cancellationToken);

        // ====================================================================
        // 4. OBTENER TRANSACCIONES DE TARJETA
        // ====================================================================
        var cardTransactions = await context.CreditCardTransaction
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Where(t => t.Account.UserID == request.UserID)
            .ToListAsync(cancellationToken);

        // ====================================================================
        // 5. OBTENER GASTOS FIJOS ACTIVOS
        // ====================================================================
        var allFixedExpenses = await context.FixedExpense
            .AsNoTracking()
            .Include(f => f.Category)
            .Where(f => f.UserID == request.UserID && f.Active)
            .ToListAsync(cancellationToken);

        var paidFixedExpenses = await context.Transaction
            .AsNoTracking()
            .Where(t => t.Account.UserID == request.UserID && t.FixedExpenseID != null)
            .Select(t => new {
                ExpenseID = t.FixedExpenseID,
                Year = t.Date.Year,
                Month = t.Date.Month
            })
            .ToListAsync(cancellationToken);

        // ====================================================================
        // 5b. OBTENER INGRESOS FIJOS ACTIVOS
        // ====================================================================
        var allFixedIncomes = await context.FixedIncome
            .AsNoTracking()
            .Where(f => f.UserID == request.UserID && f.Active)
            .ToListAsync(cancellationToken);

        var receivedFixedIncomes = await context.Transaction
            .AsNoTracking()
            .Where(t => t.Account.UserID == request.UserID && t.FixedIncomeID != null)
            .Select(t => new {
                IncomeID = t.FixedIncomeID,
                Year = t.Date.Year,
                Month = t.Date.Month
            })
            .ToListAsync(cancellationToken);

        // ====================================================================
        // 6. CALCULAR BALANCES MENSUALES (12 MESES)
        // ====================================================================
        var startDate = new DateTime(fechaActual.Year, fechaActual.Month, 1);
        var mesProximo = startDate.AddMonths(1);
        var mesDespuesDelPago = startDate.AddMonths(2);

        var balances = new List<MonthlyBalanceDto>();
        decimal acumulado = saldoInicialTotal;

        for (int i = 0; i < 12; i++)
        {
            var m = startDate.AddMonths(i);
            var key = $"{m.Year}-{m.Month:D2}";

            // ────────────────────────────────────────────────────────────
            // A. PROYECCIONES MANUALES DEL MES (con conversión USD→ARS)
            // ────────────────────────────────────────────────────────────
            var monthTrans = manualProjections
                .Where(t => t.DateTransaction.Value.Year == m.Year && t.DateTransaction.Value.Month == m.Month)
                .ToList();

            decimal ingresos = monthTrans
                .Where(t => t.Category.Type == "Ingreso")
                .Sum(t => t.Currency == "USD" ? t.Amount * cotizacionDolar : t.Amount);

            decimal manualGastos = monthTrans
                .Where(t => t.Category.Type == "Gasto")
                .Sum(t => t.Currency == "USD" ? t.Amount * cotizacionDolar : t.Amount);

            decimal gastos = manualGastos;

            // ────────────────────────────────────────────────────────────
            // B. GASTOS FIJOS DEL MES (con StartDate y conversión USD)
            // ────────────────────────────────────────────────────────────
            var daysInMonth = DateTime.DaysInMonth(m.Year, m.Month);
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

            decimal gastosFixosDelMes = fixedExpensesOfMonth
                .Sum(f => f.Currency == "USD" ? f.Amount * cotizacionDolar : f.Amount);
            gastos += gastosFixosDelMes;

            // ────────────────────────────────────────────────────────────
            // B2. INGRESOS FIJOS DEL MES (con StartDate y conversión USD)
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

            decimal ingresosRecurrentesDelMes = fixedIncomesOfMonth
                .Sum(f => f.Currency == "USD" ? f.Amount * cotizacionDolar : f.Amount);
            ingresos += ingresosRecurrentesDelMes;

            // ────────────────────────────────────────────────────────────
            // C/D. TARJETAS: resumen próximo + proyección futura
            // ────────────────────────────────────────────────────────────
            decimal pendingInstallments = 0m;
            bool hasCreditCardPayment = monthTrans
                .Any(t => t.Category.Type == "Gasto"
                          && !string.IsNullOrEmpty(t.Description)
                          && t.Description.Contains("Total TC", StringComparison.OrdinalIgnoreCase));

            if (!hasCreditCardPayment)
            {
                if (SameMonth(m, mesProximo) && deudaResumenProximo > 0)
                    pendingInstallments += deudaResumenProximo;

                if (m >= mesDespuesDelPago)
                {
                    foreach (var cardTx in cardTransactions)
                    {
                        if (cardTx.TransactionDate == null) continue;

                        var compraMes = new DateTime(cardTx.TransactionDate.Year, cardTx.TransactionDate.Month, 1);
                        var vencimiento = compraMes.AddMonths(1);

                        decimal montoArs = (decimal)cardTx.Amount;
                        if (cardTx.Account.Currency == "USD")
                            montoArs *= cotizacionDolar;

                        bool esFijo = cardTx.Fixed;
                        bool esCuotas = cardTx.Installments > 1;
                        bool esVariable = !esFijo && !esCuotas;

                        if (esVariable)
                        {
                            if (SameMonth(m, vencimiento)) pendingInstallments += montoArs;
                            continue;
                        }
                        if (esFijo && !esCuotas)
                        {
                            pendingInstallments += montoArs;
                            continue;
                        }
                        if (esCuotas)
                        {
                            int totalCuotas = (int)cardTx.Installments;
                            int cuotaBase = (int)cardTx.ActualInstallment;
                            int offset = MonthDiff(vencimiento, m);
                            if (offset < 0) continue;
                            int cuotaEnMes = cuotaBase + offset;
                            if (cuotaEnMes > totalCuotas) continue;
                            pendingInstallments += montoArs / totalCuotas;
                        }
                    }
                }
            }

            // Añadir las cuotas calculadas a los gastos
            gastos += pendingInstallments;

            // ────────────────────────────────────────────────────────────
            // E. PERSONAS: atribuciones como ingreso del mes
            //    Lógica: si Juan debe el 50% de Netflix ($500), esos $250
            //    son un ingreso efectivo ese mes (él te los reintegrará).
            // ────────────────────────────────────────────────────────────
            decimal personsReceivable = 0m;

            // E1. Gastos fijos con persona atribuida
            foreach (var fe in fixedExpensesOfMonth.Where(f => f.PersonID != null))
            {
                var pct = fe.PersonPercentage ?? 100m;
                personsReceivable += fe.Amount * pct / 100m;
            }

            // E2. Transacciones TC con persona atribuida (solo meses futuros, mirror del loop C/D)
            if (!hasCreditCardPayment && m >= mesDespuesDelPago)
            {
                foreach (var cardTx in cardTransactions.Where(t => t.PersonID != null))
                {
                    if (cardTx.TransactionDate == null) continue;

                    var compraMesP = new DateTime(cardTx.TransactionDate.Year, cardTx.TransactionDate.Month, 1);
                    var vencimientoP = compraMesP.AddMonths(1);

                    decimal montoArsP = cardTx.Amount;
                    if (cardTx.Account.Currency == "USD")
                        montoArsP *= cotizacionDolar;

                    decimal pctP = cardTx.PersonPercentage ?? 100m;
                    bool esFijoP = cardTx.Fixed;
                    bool esCuotasP = cardTx.Installments > 1;
                    bool esVariableP = !esFijoP && !esCuotasP;

                    if (esVariableP)
                    {
                        if (SameMonth(m, vencimientoP))
                            personsReceivable += montoArsP * pctP / 100m;
                        continue;
                    }

                    if (esFijoP && !esCuotasP)
                    {
                        personsReceivable += montoArsP * pctP / 100m;
                        continue;
                    }

                    if (esCuotasP)
                    {
                        int totalCuotasP = (int)cardTx.Installments!;
                        int cuotaBaseP = (int)cardTx.ActualInstallment!;
                        int offsetP = MonthDiff(vencimientoP, m);
                        if (offsetP < 0) continue;
                        int cuotaEnMesP = cuotaBaseP + offsetP;
                        if (cuotaEnMesP > totalCuotasP) continue;
                        personsReceivable += (montoArsP / totalCuotasP) * pctP / 100m;
                    }
                }
            }

            // Las atribuciones a personas son ingresos efectivos del mes
            ingresos += personsReceivable;

            // ────────────────────────────────────────────────────────────
            // F. CALCULAR BALANCE ACUMULADO
            // ────────────────────────────────────────────────────────────
            // E. BALANCE ACUMULADO
            // gastos = manuales + fijos (sin TC); TC separado en pendingInstallments
            // ────────────────────────────────────────────────────────────
            decimal neto = ingresos - gastos - pendingInstallments;
            acumulado += neto;

            // ────────────────────────────────────────────────────────────
            // G. CONSTRUIR DTO CON FORMATO
            // F. CONSTRUIR DTO
            // ────────────────────────────────────────────────────────────
            var culture = new CultureInfo("es-AR");
            balances.Add(new MonthlyBalanceDto
            {
                Key = key,
                Label = m.ToString("MMM yy", culture).ToUpper(),
                Month = m.Month,
                Year = m.Year,
                Income = ingresos,
                IncomeFmt = ingresos.ToString("C", culture),
                Expense = gastos,
                ExpenseFmt = gastos.ToString("C", culture),
                ManualExpenses = manualGastos,
                ManualExpensesFmt = manualGastos.ToString("C", culture),
                FixedExpenses = gastosFixosDelMes,
                FixedExpensesFmt = gastosFixosDelMes.ToString("C", culture),
                PendingInstallments = pendingInstallments,
                InstallmentsFmt = pendingInstallments.ToString("C", culture),
                HasCreditCardPayment = hasCreditCardPayment,
                PersonsReceivable = personsReceivable,
                PersonsReceivableFmt = personsReceivable.ToString("C", culture),
                Balance = acumulado,
                BalanceFmt = acumulado.ToString("C", culture)
            });
        }

        return balances;
    }

    private bool SameMonth(DateTime a, DateTime b) => a.Year == b.Year && a.Month == b.Month;
    private int MonthDiff(DateTime from, DateTime to) => (to.Year - from.Year) * 12 + (to.Month - from.Month);
}
