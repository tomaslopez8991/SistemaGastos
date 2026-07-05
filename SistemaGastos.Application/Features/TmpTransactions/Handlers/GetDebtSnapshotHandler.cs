using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.TmpTransactions.Queries;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Enums;

namespace SistemaGastos.Application.Features.TmpTransactions.Handlers;

public class GetDebtSnapshotHandler(
    IApplicationDbContext context,
    IMediator mediator,
    IAccountInterestService interestService)
    : IRequestHandler<GetDebtSnapshotQuery, DebtSnapshotDto>
{
    /// <summary>Día de vencimiento a usar cuando la cuenta de TC no tiene DueDay configurado.</summary>
    private const int FallbackDueDay = 10;

    public async Task<DebtSnapshotDto> Handle(GetDebtSnapshotQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.Now.Date;
        var currentMonthStart = new DateTime(today.Year, today.Month, 1);
        var nextMonth = currentMonthStart.AddMonths(1);

        // ────────────────────────────────────────────────────────────
        // A. INGRESO Y EXCEDENTE PROYECTADO DEL PRÓXIMO MES
        //    Reutiliza el cálculo ya validado de GetProjectedBalances
        //    en lugar de duplicar la lógica de ingresos/gastos/cuotas.
        // ────────────────────────────────────────────────────────────
        var balances = await mediator.Send(new GetProjectedBalancesQuery(request.UserID), cancellationToken);

        var currentEntry = balances.FirstOrDefault(b => b.Year == currentMonthStart.Year && b.Month == currentMonthStart.Month);
        var nextEntry = balances.FirstOrDefault(b => b.Year == nextMonth.Year && b.Month == nextMonth.Month);

        decimal projectedIncomeNextMonth = nextEntry?.Income ?? 0m;
        decimal projectedSurplusNextMonth = (currentEntry != null && nextEntry != null)
            ? nextEntry.Balance - currentEntry.Balance
            : 0m;

        // ────────────────────────────────────────────────────────────
        // B. INTERÉS POR DESCUBIERTO ACUMULADO
        // ────────────────────────────────────────────────────────────
        decimal overdraftInterest = await interestService.GetTotalAccruedInterestAsync(request.UserID, cancellationToken);

        // ────────────────────────────────────────────────────────────
        // C. GASTOS FIJOS PENDIENTES DEL MES ACTUAL
        // ────────────────────────────────────────────────────────────
        var daysInMonth = DateTime.DaysInMonth(currentMonthStart.Year, currentMonthStart.Month);

        var allFixedExpenses = await context.FixedExpense
            .AsNoTracking()
            .Where(f => f.UserID == request.UserID && f.Active)
            .ToListAsync(cancellationToken);

        var paidThisMonthIds = await context.Transaction
            .AsNoTracking()
            .Where(t => t.Account.UserID == request.UserID
                        && t.FixedExpenseID != null
                        && t.Date.Year == currentMonthStart.Year
                        && t.Date.Month == currentMonthStart.Month)
            .Select(t => t.FixedExpenseID!.Value)
            .ToListAsync(cancellationToken);

        var pendingFixedExpenses = allFixedExpenses
            .Where(f => f.PaymentDay > 0
                     && f.PaymentDay <= daysInMonth
                     && !paidThisMonthIds.Contains(f.ID)
                     && (f.StartDate == null || new DateTime(f.StartDate.Value.Year, f.StartDate.Value.Month, 1) <= currentMonthStart))
            .Select(f => new PendingFixedExpenseDto
            {
                ID = f.ID,
                Name = f.Name,
                Amount = f.Amount,
                Currency = f.Currency,
                PaymentDay = f.PaymentDay
            })
            .OrderBy(f => f.PaymentDay)
            .ToList();

        // ────────────────────────────────────────────────────────────
        // D. DEUDA POR TARJETA (saldo, próximo vencimiento, cuotas restantes)
        // ────────────────────────────────────────────────────────────
        var cardAccounts = await context.Account
            .AsNoTracking()
            .Where(a => a.UserID == request.UserID && a.Type == AccountType.TarjetaCredito)
            .ToListAsync(cancellationToken);

        var cardTransactions = await context.CreditCardTransaction
            .AsNoTracking()
            .Where(t => t.Account!.UserID == request.UserID && t.Installments != null && t.Installments > 1)
            .ToListAsync(cancellationToken);

        var cardDebts = new List<CardDebtItemDto>();

        foreach (var account in cardAccounts.Where(a => a.Balance != 0))
        {
            var dueDay = Math.Min(account.DueDay ?? FallbackDueDay, DateTime.DaysInMonth(today.Year, today.Month));
            var nextDueDate = new DateTime(today.Year, today.Month, dueDay);
            if (nextDueDate < today)
                nextDueDate = nextDueDate.AddMonths(1);

            var installmentsOfAccount = cardTransactions.Where(t => t.AccountID == account.ID);

            int remainingCount = 0;
            decimal remainingTotal = 0m;

            foreach (var tx in installmentsOfAccount)
            {
                var compraMes = new DateTime(tx.TransactionDate.Year, tx.TransactionDate.Month, 1);
                var vencimiento = compraMes.AddMonths(1);
                int offsetToNow = Math.Max(MonthDiff(vencimiento, currentMonthStart), 0);
                int cuotaActualAlHoy = tx.ActualInstallment!.Value + offsetToNow;
                int cuotasRestantes = tx.Installments!.Value - cuotaActualAlHoy + 1;

                if (cuotasRestantes <= 0) continue;

                remainingCount++;
                remainingTotal += (tx.Amount / tx.Installments.Value) * cuotasRestantes;
            }

            cardDebts.Add(new CardDebtItemDto
            {
                AccountID = account.ID,
                AccountName = account.Name,
                Currency = account.Currency,
                CurrentBalance = Math.Abs(account.Balance),
                NextDueDate = nextDueDate,
                InstallmentsRemainingCount = remainingCount,
                RemainingInstallmentsTotal = remainingTotal
            });
        }

        return new DebtSnapshotDto
        {
            ProjectedIncomeNextMonth = projectedIncomeNextMonth,
            ProjectedSurplusNextMonth = projectedSurplusNextMonth,
            OverdraftInterestAccrued = overdraftInterest,
            PendingFixedExpenses = pendingFixedExpenses,
            CardDebts = cardDebts.OrderByDescending(c => c.CurrentBalance).ToList()
        };
    }

    private static int MonthDiff(DateTime from, DateTime to) => (to.Year - from.Year) * 12 + (to.Month - from.Month);
}
