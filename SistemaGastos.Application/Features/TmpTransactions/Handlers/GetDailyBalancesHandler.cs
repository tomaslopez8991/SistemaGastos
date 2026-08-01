using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.TmpTransactions.Queries;
using SistemaGastos.Application.Helpers;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Enums;
using System.Globalization;
using System.Text.Json;

namespace SistemaGastos.Application.Features.TmpTransactions.Handlers;

public class GetDailyBalancesHandler(
    IApplicationDbContext context,
    IDolarService dolarService,
    IAccountInterestService accountInterestService)
    : IRequestHandler<GetDailyBalancesQuery, DailyCalendarDto>
{
    /// <summary>Día de vencimiento a usar cuando la cuenta de TC no tiene DueDay configurado.</summary>
    private const int FallbackDueDay = 10;

    public async Task<DailyCalendarDto> Handle(GetDailyBalancesQuery request, CancellationToken cancellationToken)
    {
        await accountInterestService.RunAccrualAsync(cancellationToken);
        var fechaActual = DateTime.Now;
        var culture = new CultureInfo("es-AR");

        // El call HTTP al dólar corre en paralelo con las queries de DB.
        // EF Core DbContext no es thread-safe: las queries de DB van secuenciales.
        var dolarTask = dolarService.GetDolarBolsaAsync();

        // Opción A: avanzar GastoFijo TC con PaymentDay anterior a hoy al día actual.
        // Esto implementa el rolling automático: si ayer no se pagó, el ítem aparece hoy.
        var todayForRoll = DateTime.Today;
        if (request.Year == todayForRoll.Year && request.Month == todayForRoll.Month)
        {
            var monthKey = $"{todayForRoll.Year}-{todayForRoll.Month:D2}";
            var staleFeList = await context.FixedExpense
                .Where(f => f.UserID == request.UserID
                         && f.CreditCardAccountID != null
                         && f.PaymentYearMonth == monthKey
                         && f.PaymentDay < todayForRoll.Day
                         && f.Amount > 0
                         && f.Active)
                .ToListAsync(cancellationToken);

            if (staleFeList.Count > 0)
            {
                foreach (var fe in staleFeList)
                    fe.PaymentDay = todayForRoll.Day;
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        var accounts = await context.Account
            .Where(a => a.UserID == request.UserID)
            .ToListAsync(cancellationToken);

        var manualProjections = await context.TmpTransaction
            .Include(t => t.Category)
            .Where(t => t.UserID == request.UserID && t.DateTransaction.HasValue)
            .ToListAsync(cancellationToken);

        var cardTransactions = await context.CreditCardTransaction
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.SharedWith).ThenInclude(s => s.Person)
            .Where(t => t.Account.UserID == request.UserID)
            .ToListAsync(cancellationToken);

        var allFixedExpenses = await context.FixedExpense
            .AsNoTracking()
            .Include(f => f.Category)
            .Include(f => f.Person)
            .Where(f => f.UserID == request.UserID && f.Active)
            .ToListAsync(cancellationToken);

        // Cuentas TC que ya tienen un registro de pago generado para un mes específico.
        // Clave: "{ccAccountId}_{YYYY-MM}" — usada en Section C para no duplicar en el calendario.
        var closedTcKeys = allFixedExpenses
            .Where(f => f.CreditCardAccountID.HasValue && !string.IsNullOrEmpty(f.PaymentYearMonth))
            .Select(f => $"{f.CreditCardAccountID}_{f.PaymentYearMonth}")
            .ToHashSet();

        var paidFixedExpenses = await context.Transaction
            .AsNoTracking()
            .Where(t => t.Account.UserID == request.UserID
                     && t.FixedExpenseID != null
                     && (t.FixedExpense!.CreditCardAccountID == null || t.FixedExpense.Amount <= 0))
            .Select(t => new { ExpenseID = t.FixedExpenseID, Year = t.Date.Year, Month = t.Date.Month })
            .ToListAsync(cancellationToken);

        var allFixedIncomes = await context.FixedIncome
            .AsNoTracking()
            .Where(f => f.UserID == request.UserID && f.Active)
            .ToListAsync(cancellationToken);

        var personsWithCollection = await context.Person
            .Where(p => p.UserID == request.UserID && p.Active && p.CollectionDay != null)
            .ToListAsync(cancellationToken);

        var interestSettings = await context.AccountInterestSetting
            .AsNoTracking()
            .Where(s => s.UserID == request.UserID && s.Enabled)
            .ToListAsync(cancellationToken);

        var receivedFixedIncomes = await context.Transaction
            .AsNoTracking()
            .Where(t => t.Account.UserID == request.UserID && t.FixedIncomeID != null)
            .Select(t => new { IncomeID = t.FixedIncomeID, Year = t.Date.Year, Month = t.Date.Month })
            .ToListAsync(cancellationToken);

        decimal cotizacionDolar = await dolarTask;

        // ====================================================================
        // 1. SALDO INICIAL
        // ====================================================================
        var saldoLiquidezARS = accounts
            .Where(a => a.Type != AccountType.TarjetaCredito && a.Currency == "ARS")
            .Sum(a => a.Balance);

        var saldoLiquidezUSD = accounts
            .Where(a => a.Type != AccountType.TarjetaCredito && (a.Currency == "USD" || a.Currency == "USDT"))
            .Sum(a => a.Balance);

        decimal saldoInicialTotal = saldoLiquidezARS + (saldoLiquidezUSD * cotizacionDolar);
        var projectedInterestBalances = interestSettings.ToDictionary(
            s => s.AccountID,
            s => accounts.FirstOrDefault(a => a.ID == s.AccountID)?.Balance ?? 0m);
        var projectedInterestDue = new Dictionary<(int AccountID, int Year, int Month), decimal>();

        var ccAccounts = accounts.Where(a => a.Type == AccountType.TarjetaCredito).ToList();

        // ====================================================================
        // 2. TRANSACCIONES ATRIBUIDAS A PERSONAS (depende de personsWithCollection)
        // ====================================================================
        List<Domain.Models.Transaction> personAttributedTx = new();
        if (personsWithCollection.Count > 0)
        {
            var pIds = personsWithCollection.Select(p => p.ID).ToList();
            personAttributedTx = await context.Transaction
                .Include(t => t.Category)
                .Where(t => t.PersonID != null && pIds.Contains(t.PersonID.Value)
                         && t.Account.UserID == request.UserID)
                .ToListAsync(cancellationToken);
        }

        var personCreditCardCollections = personsWithCollection.Count == 0
            ? new List<Domain.Models.CreditCardTransactionCobro>()
            : await context.CreditCardTransactionCobro
                .Where(c => personsWithCollection.Select(p => p.ID).Contains(c.PersonID))
                .ToListAsync(cancellationToken);

        // ====================================================================
        // 3. TRANSACCIONES REALES (días pasados/mes actual)
        // ====================================================================
        var startDate     = new DateTime(fechaActual.Year, fechaActual.Month, 1);
        var requestedDate = new DateTime(request.Year, request.Month, 1);

        bool viewingPast    = requestedDate < startDate;
        bool viewingCurrent = !viewingPast && requestedDate == startDate;

        // Carga todas las transacciones desde el inicio del mes solicitado hasta hoy.
        // Para mes actual: solo las del mes actual (ya sucedidas).
        // Para mes pasado: las del mes pasado + las de todos los meses intermedios hasta hoy
        //   → permite calcular el saldo al inicio del mes pasado restando el delta acumulado.
        List<Domain.Models.Transaction> allTxsFromViewed = new();
        List<Domain.Models.Transaction> actualMonthTxs   = new();
        decimal saldoMesInicio = saldoInicialTotal;

        if (viewingPast || viewingCurrent)
        {
            allTxsFromViewed = await context.Transaction
                .Include(t => t.Category)
                .Include(t => t.Account)
                .Where(t => t.Account.UserID == request.UserID && t.Date >= requestedDate)
                .ToListAsync(cancellationToken);

            actualMonthTxs = allTxsFromViewed
                .Where(t => t.Date.Year == requestedDate.Year && t.Date.Month == requestedDate.Month)
                .ToList();

            // Saldo al inicio del mes visto = saldo actual − neto de tx desde ese mes hasta HOY (inclusive).
            // Se excluyen transacciones con fecha futura para no distorsionar el punto de partida.
            decimal netDelta = allTxsFromViewed
                .Where(t => t.Date.Date <= fechaActual.Date)
                .Sum(t => (t.Category?.Type == "Ingreso" ? 1m : -1m) * t.Amount);
            saldoMesInicio = saldoInicialTotal - netDelta;
        }

        var result = new DailyCalendarDto
        {
            Year         = requestedDate.Year,
            Month        = requestedDate.Month,
            MonthLabel   = requestedDate.ToString("MMMM yyyy", culture),
            DolarRate    = cotizacionDolar,
            IsPastMonth  = viewingPast
        };

        // ====================================================================
        // 4. RECORRER MESES HASTA EL MES SOLICITADO, ACUMULANDO SALDO
        // ====================================================================
        int monthDiff = MonthDiff(startDate, requestedDate);
        int loopFrom  = viewingPast ? monthDiff : 0;

        decimal acumulado = viewingPast ? saldoMesInicio : saldoInicialTotal;

        for (int i = loopFrom; i <= monthDiff; i++)
        {
            var m = startDate.AddMonths(i);
            var daysInMonth = DateTime.DaysInMonth(m.Year, m.Month);
            var itemsByDay = new Dictionary<int, List<DailyBalanceItemDto>>();

            void AddItem(int day, string description, decimal amount, bool isIncome, string sourceType, long? sourceId = null, bool isDistributed = false, int? tcAccountId = null, decimal? tcMinimumAmount = null, decimal? tcTotalAmount = null, bool isAutomaticPersonCollection = false)
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
                    SourceId = sourceId,
                    Description = description,
                    Amount = amount,
                    AmountFmt = amount.ToString("C", culture),
                    IsIncome = isIncome,
                    SourceType = sourceType,
                    Day = day,
                    IsDistributed = isDistributed,
                    TcAccountId = tcAccountId,
                    TcTotalAmount = tcTotalAmount,
                    IsAutomaticPersonCollection = isAutomaticPersonCollection,
                    TcMinimumAmount = tcMinimumAmount
                });
            }

            static Dictionary<string, decimal> ParseDayOverrides(string? json)
            {
                if (string.IsNullOrEmpty(json)) return new();
                try { return JsonSerializer.Deserialize<Dictionary<string, decimal>>(json) ?? new(); }
                catch { return new(); }
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

                bool isDistributed = dist.Count > 1;
                var overrides = ParseDayOverrides(t.DayAmountOverrides);

                foreach (var (kv, idx) in dist.Select((kv, idx) => (kv, idx)))
                {
                    var dayAmount = overrides.TryGetValue(kv.Key.ToString(), out var ov) ? ov : kv.Value;
                    var desc = isDistributed ? $"{t.Description} (día {idx + 1}/{dist.Count})" : t.Description;
                    AddItem(kv.Key, desc, dayAmount, isIncome, "Planificado", t.ID, isDistributed);
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

            var monthKey = $"{m.Year}-{m.Month:D2}";
            var fixedExpensesOfMonth = allFixedExpenses
                .Where(f => f.PaymentDay > 0
                         && f.PaymentDay <= daysInMonth
                         && !paidThisMonthIds.Contains(f.ID)
                         && (f.StartDate == null || new DateTime(f.StartDate.Value.Year, f.StartDate.Value.Month, 1) <= m)
                         && (string.IsNullOrEmpty(f.PausedMonths) || !f.PausedMonths.Split(',').Select(s => s.Trim()).Contains(monthKey))
                         && (f.PaymentYearMonth == null || f.PaymentYearMonth == monthKey))
                .ToList();

            foreach (var fe in fixedExpensesOfMonth)
            {
                decimal amountArs = fe.Currency == "USD" ? fe.Amount * cotizacionDolar : fe.Amount;

                // Metadatos TC para GastoFijo que representa el pago de una TC
                int? feTcAccountId = null;
                decimal? feTcMinimumAmount = null;
                decimal? feTcTotalAmount = null;
                string? feTcCurrency = null;
                if (fe.CreditCardAccountID.HasValue)
                {
                    var ccForFe = ccAccounts.FirstOrDefault(a => a.ID == fe.CreditCardAccountID.Value);
                    if (ccForFe != null)
                    {
                        feTcAccountId  = ccForFe.ID;
                        feTcCurrency   = ccForFe.Currency;
                        feTcMinimumAmount = ccForFe.EffectiveMinimumPayment.HasValue
                            ? (ccForFe.Currency == "USD" ? ccForFe.EffectiveMinimumPayment.Value * cotizacionDolar : ccForFe.EffectiveMinimumPayment.Value)
                            : null;
                        feTcTotalAmount = amountArs;
                    }
                }

                var excludedDays = DistributionHelper.ParseExcludedDays(fe.ExcludedDays);
                var dist = DistributionHelper.Distribute(amountArs, fe.PaymentDay, fe.DistributionEndDay, excludedDays, daysInMonth)
                    .OrderBy(kv => kv.Key).ToList();

                foreach (var (kv, idx) in dist.Select((kv, idx) => (kv, idx)))
                {
                    var baseName = feTcCurrency != null
                        ? $"{fe.Name} ({feTcCurrency})"
                        : fe.Name;
                    var desc = dist.Count > 1 ? $"{baseName} (día {idx + 1}/{dist.Count})" : baseName;
                    AddItem(kv.Key, desc, kv.Value, false, "GastoFijo", (long)fe.ID,
                        tcAccountId: feTcAccountId, tcMinimumAmount: feTcMinimumAmount, tcTotalAmount: feTcTotalAmount);
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
                         && (f.PersonID == null || f.CollectionYearMonth == monthKey)
                         && !receivedThisMonthIds.Contains(f.ID)
                         && !(f.PersonID.HasValue && f.LastGeneratedDate.HasValue)
                         && (f.StartDate == null || new DateTime(f.StartDate.Value.Year, f.StartDate.Value.Month, 1) <= m)
                         && (string.IsNullOrEmpty(f.PausedMonths) || !f.PausedMonths.Split(',').Select(s => s.Trim()).Contains(monthKey)))
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
                    AddItem(kv.Key, desc, kv.Value, true, "IngresoFijo", (long)fi.ID,
                        dist.Count > 1, isAutomaticPersonCollection: fi.PersonID.HasValue);
                }
            }

            // ────────────────────────────────────────────────────────────
            // C. TOTAL TC — mes de vencimiento inmediato de cada cuenta.
            // Usa el balance actual de la cuenta TC (saldo real acumulado),
            // que es exactamente lo que el usuario deberá pagar el próximo vencimiento.
            // ────────────────────────────────────────────────────────────
            if (!hasCreditCardPayment)
            {
                foreach (var cc in ccAccounts)
                {
                    var ccDueMonth = startDate.AddMonths(cc.DueMonthOffset ?? 1);
                    if (!SameMonth(m, ccDueMonth)) continue;
                    if (closedTcKeys.Contains($"{cc.ID}_{m.Year}-{m.Month:D2}")) continue;

                    decimal balanceArs = cc.Currency == "USD"
                        ? cc.EffectiveTcProjection * cotizacionDolar
                        : cc.EffectiveTcProjection;

                    if (balanceArs <= 0) continue;
                    var tcLabel = cc.TcProjectionMode == Domain.Enums.TcProjectionMode.Total
                        ? $"Total TC - {cc.Name} ({cc.Currency})"
                        : cc.TcProjectionMode == Domain.Enums.TcProjectionMode.Minimo
                            ? $"Mínimo TC - {cc.Name} ({cc.Currency})"
                            : $"Pago TC - {cc.Name} ({cc.Currency})";
                    decimal? minArs = cc.EffectiveMinimumPayment.HasValue
                        ? (cc.Currency == "USD" ? cc.EffectiveMinimumPayment.Value * cotizacionDolar : cc.EffectiveMinimumPayment.Value)
                        : null;
                    decimal totalArs = cc.Currency == "USD" ? Math.Abs(cc.Balance) * cotizacionDolar : Math.Abs(cc.Balance);
                    AddItem(cc.DueDay ?? FallbackDueDay, tcLabel, balanceArs, false, "TarjetaCredito",
                        tcAccountId: cc.ID, tcMinimumAmount: minArs, tcTotalAmount: totalArs);
                }
            }

            // ────────────────────────────────────────────────────────────
            // D. TOTAL TC FUTURO — cuotas y cargos por tarjeta
            // Solo para meses posteriores al vencimiento inmediato de cada cuenta.
            // ────────────────────────────────────────────────────────────
            if (!hasCreditCardPayment && m > startDate)
            {
                var totalesPorCuenta = new Dictionary<int, decimal>();

                foreach (var cardTx in cardTransactions)
                {
                    // Excluir el mes cubierto por Section C para esta cuenta
                    var acctDueMonth = startDate.AddMonths(cardTx.Account.DueMonthOffset ?? 1);
                    if (SameMonth(m, acctDueMonth)) continue;

                    var compraMes = new DateTime(cardTx.TransactionDate.Year, cardTx.TransactionDate.Month, 1);

                    var closingDay = cardTx.Account.ClosingDay;
                    var mesesAlCierre = (closingDay.HasValue && cardTx.TransactionDate.Day > closingDay.Value) ? 1 : 0;
                    var mesResumen = compraMes.AddMonths(mesesAlCierre);
                    var vencimiento = mesResumen.AddMonths(cardTx.Account.DueMonthOffset ?? 1);

                    decimal montoArs = cardTx.Account.Currency == "USD" ? cardTx.Amount * cotizacionDolar : cardTx.Amount;

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
                }

                foreach (var (accountId, total) in totalesPorCuenta)
                {
                    var cc = ccAccounts.FirstOrDefault(a => a.ID == accountId);
                    if (cc == null) continue;

                    decimal? minArsD = cc.EffectiveMinimumPayment.HasValue
                        ? (cc.Currency == "USD" ? cc.EffectiveMinimumPayment.Value * cotizacionDolar : cc.EffectiveMinimumPayment.Value)
                        : null;
                    AddItem(cc.DueDay ?? FallbackDueDay, $"Total TC - {cc.Name} ({cc.Currency})", total, false, "TarjetaCredito",
                        tcAccountId: cc.ID, tcMinimumAmount: minArsD, tcTotalAmount: total);
                }
            }

            // ────────────────────────────────────────────────────────────
            // E. COBROS DE PERSONAS
            // Lógica alineada con GetPersonAccountsHandler:
            //   - CC atribuidas: all-time (deuda acumulada permanente)
            //   - FE atribuidos: solo los activos en el mes m
            //   - Transacciones: solo las del mes m (gastos suman, cobros restan)
            // ────────────────────────────────────────────────────────────
            foreach (var person in personsWithCollection)
            {
                // Cuando el cierre ya congeló la cuenta como ingreso mensual,
                // ese snapshot reemplaza al cálculo dinámico del calendario.
                if (allFixedIncomes.Any(f => f.PersonID == person.ID
                                          && f.CollectionYearMonth == monthKey))
                    continue;

                // Respetar mes de inicio configurado
                if (!string.IsNullOrEmpty(person.CollectionFrom))
                {
                    var parts = person.CollectionFrom.Split('-');
                    if (parts.Length == 2
                        && int.TryParse(parts[0], out int fromYear)
                        && int.TryParse(parts[1], out int fromMonth)
                        && (m.Year < fromYear || (m.Year == fromYear && m.Month < fromMonth)))
                        continue;
                }

                var monthWasCollected = !string.IsNullOrEmpty(person.CollectedMonths)
                    && person.CollectedMonths.Split(',').Select(s => s.Trim()).Contains(monthKey);
                var collectionCutoff = personAttributedTx
                    .Where(t => t.PersonID == person.ID
                             && t.Date < m.AddMonths(1)
                             && t.Category?.Type == "Ingreso"
                             && t.Description.StartsWith("Cobro:", StringComparison.OrdinalIgnoreCase)
                             && !personCreditCardCollections.Any(c => c.PersonID == person.ID
                                                                   && Math.Abs((c.CreatedAt - t.Date).TotalSeconds) <= 5))
                    .Select(t => (DateTime?)t.Date)
                    .Max();

                // Compatibilidad con cobros antiguos que sólo marcaron el mes.
                if (monthWasCollected && !collectionCutoff.HasValue)
                    continue;

                // CC all-time
                decimal ccBalance = cardTransactions
                    .Where(cc => cc.SharedWith.Any(s => s.PersonID == person.ID)
                              && PersonCreditCardBalanceHelper.ShouldInclude(
                                  cc,
                                  m,
                                  collectionCutoff,
                                  personCreditCardCollections
                                      .Where(c => c.PersonID == person.ID && c.CreditCardTransactionID == cc.ID)
                                      .Select(c => c.CreatedAt)))
                    .Sum(cc => (cc.Account?.Currency == "USD" ? cc.Amount * cotizacionDolar : cc.Amount)
                             * (cc.SharedWith.FirstOrDefault(s => s.PersonID == person.ID)?.Percentage ?? 100m) / 100m);

                // FE del mes m
                decimal feBalance = allFixedExpenses
                    .Where(f => f.PersonID == person.ID
                             && (f.PaymentYearMonth == null || f.PaymentYearMonth == monthKey)
                             && (f.StartDate == null || new DateTime(f.StartDate.Value.Year, f.StartDate.Value.Month, 1) <= m)
                             && (!collectionCutoff.HasValue || (f.StartDate.HasValue && f.StartDate.Value > collectionCutoff.Value))
                             && (string.IsNullOrEmpty(f.PausedMonths) || !f.PausedMonths.Split(',').Select(s => s.Trim()).Contains(monthKey)))
                    .Sum(f => {
                        decimal amtArs = f.Currency == "USD" ? f.Amount * cotizacionDolar : f.Amount;
                        return amtArs * (f.PersonPercentage ?? 100m) / 100m;
                    });

                // Transacciones del mes m: gastos suman, cobros (ingresos) restan
                var monthPersonTx = personAttributedTx
                    .Where(t => t.PersonID == person.ID
                             && t.Date.Year == m.Year && t.Date.Month == m.Month
                             && (!collectionCutoff.HasValue || t.Date > collectionCutoff.Value));
                decimal txExpenses = monthPersonTx.Where(t => t.Category?.Type != "Ingreso")
                    .Sum(t => t.Amount * (t.PersonPercentage ?? 100m) / 100m);
                decimal txPayments = monthPersonTx.Where(t => t.Category?.Type == "Ingreso")
                    .Sum(t => t.Amount * (t.PersonPercentage ?? 100m) / 100m);

                decimal personBalance = ccBalance + feBalance + txExpenses - txPayments;
                var hasPriorFullCollection = personAttributedTx.Any(t =>
                    t.PersonID == person.ID
                    && t.Date < m
                    && t.Category?.Type == "Ingreso"
                    && t.Description.StartsWith("Cobro:", StringComparison.OrdinalIgnoreCase));
                decimal netOwed = personBalance - (hasPriorFullCollection ? 0m : person.DiscountAmount ?? 0m);
                if (netOwed <= 0) continue;

                AddItem(person.CollectionDay!.Value, $"Cobro: {person.Name}", netOwed, true, "Personas", (long)person.ID);
            }

            // ────────────────────────────────────────────────────────────
            // TOTAL DEL MES Y ACUMULADO
            // ────────────────────────────────────────────────────────────
            if (m > startDate)
            {
                foreach (var setting in interestSettings)
                {
                    projectedInterestBalances.TryGetValue(setting.AccountID, out var projectedBalance);
                    var account = accounts.FirstOrDefault(a => a.ID == setting.AccountID);
                    if (account == null) continue;
                    projectedInterestDue.TryGetValue((setting.AccountID, m.Year, m.Month), out var dueInterest);
                    if (dueInterest > 0)
                        AddItem(7, $"Intereses estimados - {account.Name}", dueInterest, false, "InteresEstimado");

                    decimal estimatedInterest = 0m;
                    decimal? previousBalance = null;
                    int sameBalanceDays = 0;

                    for (int day = 1; day <= daysInMonth; day++)
                    {
                        var fixedIncomeDelta = allFixedIncomes
                            .Where(f => f.AccountID == setting.AccountID
                                     && f.PersonID == null
                                     && f.ReceiptDay == day
                                     && (f.StartDate == null || new DateTime(f.StartDate.Value.Year, f.StartDate.Value.Month, 1) <= m)
                                     && (string.IsNullOrEmpty(f.PausedMonths) || !f.PausedMonths.Split(',').Select(x => x.Trim()).Contains(monthKey)))
                            .Sum(f => f.Currency == "USD" ? f.Amount * cotizacionDolar : f.Amount);
                        var fixedExpenseDelta = allFixedExpenses
                            .Where(f => f.AccountID == setting.AccountID
                                     && f.PaymentDay == day
                                     && (f.PaymentYearMonth == null || f.PaymentYearMonth == monthKey)
                                     && (f.StartDate == null || new DateTime(f.StartDate.Value.Year, f.StartDate.Value.Month, 1) <= m)
                                     && (string.IsNullOrEmpty(f.PausedMonths) || !f.PausedMonths.Split(',').Select(x => x.Trim()).Contains(monthKey)))
                            .Sum(f => f.Currency == "USD" ? f.Amount * cotizacionDolar : f.Amount);
                        var manualIncomeDelta = monthTrans
                            .Where(t => t.AccountID == setting.AccountID && t.DateTransaction!.Value.Day == day && t.Category.Type == "Ingreso")
                            .Sum(t => t.Currency == "USD" ? t.Amount * cotizacionDolar : t.Amount);
                        var manualExpenseDelta = monthTrans
                            .Where(t => t.AccountID == setting.AccountID && t.DateTransaction!.Value.Day == day && t.Category.Type != "Ingreso")
                            .Sum(t => t.Currency == "USD" ? t.Amount * cotizacionDolar : t.Amount);

                        var estimatedInterestPayment = day == 7 ? dueInterest : 0m;
                        projectedBalance += fixedIncomeDelta + manualIncomeDelta
                                          - fixedExpenseDelta - manualExpenseDelta - estimatedInterestPayment;
                        sameBalanceDays = previousBalance.HasValue && projectedBalance == previousBalance.Value
                            ? sameBalanceDays + 1
                            : 1;
                        previousBalance = projectedBalance;

                        if (projectedBalance < 0)
                            estimatedInterest += Math.Round(Math.Abs(projectedBalance) * setting.InterestRate * sameBalanceDays / 365m, 2);
                    }

                    estimatedInterest = Math.Round(estimatedInterest, 2);
                    if (estimatedInterest > 0)
                    {
                        var nextMonth = m.AddMonths(1);
                        projectedInterestDue[(setting.AccountID, nextMonth.Year, nextMonth.Month)] = estimatedInterest;
                    }

                    projectedInterestBalances[setting.AccountID] = projectedBalance;
                }
            }

            decimal netoDelMes = itemsByDay.Values
                .SelectMany(items => items)
                .Sum(item => item.IsIncome ? item.Amount : -item.Amount);

            if (i == monthDiff)
            {
                if (viewingPast)
                {
                    // Mes anterior: solo días con ítems pendientes (sin cálculo de saldo).
                    foreach (var kvp in itemsByDay.OrderBy(kv => kv.Key))
                    {
                        var dayItems = kvp.Value.OrderBy(x => x.IsIncome ? 0 : 1).ToList();
                        result.Days.Add(new DailyBalanceDto
                        {
                            Day        = kvp.Key,
                            Date       = new DateTime(m.Year, m.Month, kvp.Key).ToString("yyyy-MM-dd"),
                            Income     = dayItems.Where(x => x.IsIncome).Sum(x => x.Amount),
                            Expense    = dayItems.Where(x => !x.IsIncome).Sum(x => x.Amount),
                            Balance    = 0,
                            BalanceFmt = "",
                            Items      = dayItems
                        });
                    }
                    result.HasPendingItems = result.Days.Count > 0;
                }
                else
                {
                    // Mes actual o futuro: saldo inicial + loop día a día
                    decimal startBal = viewingCurrent ? saldoMesInicio : acumulado;
                    result.StartingBalance    = startBal;
                    result.StartingBalanceFmt = startBal.ToString("C", culture);

                    decimal running = startBal;
                    var todayDate = fechaActual.Date;

                    for (int day = 1; day <= daysInMonth; day++)
                    {
                        List<DailyBalanceItemDto> items;

                        if (viewingCurrent && new DateTime(m.Year, m.Month, day).Date <= todayDate)
                        {
                            // Días pasados/hoy del mes actual: balance real + items proyectados no pagados
                            var dayTxs = actualMonthTxs.Where(t => t.Date.Day == day).ToList();
                            decimal inc = dayTxs.Where(t => t.Category?.Type == "Ingreso").Sum(t => t.Amount);
                            decimal exp = dayTxs.Where(t => t.Category?.Type != "Ingreso").Sum(t => t.Amount);
                            running += inc - exp;

                            var pastItems = itemsByDay.TryGetValue(day, out var pastProjList)
                                ? pastProjList.OrderBy(x => x.IsIncome ? 0 : 1).ToList()
                                : new List<DailyBalanceItemDto>();

                            decimal planInc = pastItems.Where(x => x.IsIncome).Sum(x => x.Amount);
                            decimal planExp = pastItems.Where(x => !x.IsIncome).Sum(x => x.Amount);
                            running += planInc - planExp;

                            result.Days.Add(new DailyBalanceDto
                            {
                                Day        = day,
                                Date       = new DateTime(m.Year, m.Month, day).ToString("yyyy-MM-dd"),
                                Income     = inc + planInc,
                                Expense    = exp + planExp,
                                Balance    = running,
                                BalanceFmt = running.ToString("C", culture),
                                Items      = pastItems
                            });
                            continue;
                        }

                        // Días futuros: proyecciones
                        items = itemsByDay.TryGetValue(day, out var projList)
                            ? projList.OrderBy(x => x.IsIncome ? 0 : 1).ToList()
                            : new List<DailyBalanceItemDto>();

                        decimal income  = items.Where(x => x.IsIncome).Sum(x => x.Amount);
                        decimal expense = items.Where(x => !x.IsIncome).Sum(x => x.Amount);
                        running += income - expense;

                        result.Days.Add(new DailyBalanceDto
                        {
                            Day        = day,
                            Date       = new DateTime(m.Year, m.Month, day).ToString("yyyy-MM-dd"),
                            Income     = income,
                            Expense    = expense,
                            Balance    = running,
                            BalanceFmt = running.ToString("C", culture),
                            Items      = items
                        });
                    }
                }
            }

            acumulado += netoDelMes;
        }

        return result;
    }

    private bool SameMonth(DateTime a, DateTime b) => a.Year == b.Year && a.Month == b.Month;
    private int MonthDiff(DateTime from, DateTime to) => (to.Year - from.Year) * 12 + (to.Month - from.Month);

}
