using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.TmpTransactions.Queries;
using SistemaGastos.Application.Interfaces;
using System.Globalization;

namespace SistemaGastos.Application.Features.TmpTransactions.Handlers;

public class GetBalancesHandler(IApplicationDbContext context)
    : IRequestHandler<GetBalancesQuery, List<MonthBalanceDto>>
{
    public async Task<List<MonthBalanceDto>> Handle(GetBalancesQuery request, CancellationToken cancellationToken)
    {
        var culture = new CultureInfo("es-AR");
        var now = DateTime.Now;
        var result = new List<MonthBalanceDto>();

        // Calcular saldo actual de todas las cuentas
        var currentBalance = await context.Account
            .Where(a => a.UserID == request.UserID)
            .SumAsync(a => a.Balance, cancellationToken);

        // Obtener todas las transacciones proyectadas
        var allTmpTransactions = await context.TmpTransaction
            .AsNoTracking()
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Where(t => t.Account.UserID == request.UserID)
            .ToListAsync(cancellationToken);

        // ✅ Obtener todas las compras en cuotas pendientes
        var allCreditCardTransactions = await context.CreditCardTransaction
            .AsNoTracking()
            .Include(c => c.Account)
            .Where(c => c.Account.UserID == request.UserID
                && c.ActualInstallment < c.Installments) // Tienen cuotas pendientes
            .ToListAsync(cancellationToken);

        // Proyectar próximos 12 meses
        for (int i = 0; i < 12; i++)
        {
            var targetDate = now.AddMonths(i);
            var startOfMonth = new DateTime(targetDate.Year, targetDate.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            // Filtrar transacciones del mes
            var tmpTransactionsMonth = allTmpTransactions
                .Where(t => t.DateTransaction >= startOfMonth && t.DateTransaction <= endOfMonth)
                .ToList();

            // Calcular ingresos y gastos
            var income = tmpTransactionsMonth
                .Where(t => t.Category.Type == "Ingreso")
                .Sum(t => t.Amount);

            var expense = tmpTransactionsMonth
                .Where(t => t.Category.Type == "Gasto")
                .Sum(t => t.Amount);

            // ✅ Verificar si hay "Total TC" proyectado
            var hasCreditCardPayment = tmpTransactionsMonth
                .Any(t => t.Category.Type == "Gasto"
                    && !string.IsNullOrEmpty(t.Description)
                    && t.Description.Contains("Total TC", StringComparison.OrdinalIgnoreCase));

            decimal pendingInstallments = 0;

            if (!hasCreditCardPayment)
            {
                // ✅ Calcular cuotas que vencen en este mes
                foreach (var cc in allCreditCardTransactions)
                {
                    // Calcular cuántos meses pasaron desde la compra
                    var monthsSincePurchase = (targetDate.Year - cc.TransactionDate.Year) * 12 + (targetDate.Month - cc.TransactionDate.Month);

                    // Si hay una cuota que vence en este mes específico
                    if (monthsSincePurchase >= 0
                        && monthsSincePurchase < cc.Installments
                        && monthsSincePurchase >= cc.ActualInstallment)
                    {
                        pendingInstallments += cc.Amount;
                    }
                }
            }

            // Calcular balance acumulado
            var balance = currentBalance + income - expense - pendingInstallments;
            currentBalance = balance; // Acumular para siguiente mes

            result.Add(new MonthBalanceDto
            {
                Key = $"{targetDate.Year}-{targetDate.Month:D2}",
                Label = targetDate.ToString("MMM yyyy", culture),
                Balance = balance,
                BalanceFmt = balance.ToString("C", culture),
                Income = income,
                IncomeFmt = income.ToString("C", culture),
                Expense = expense,
                ExpenseFmt = expense.ToString("C", culture),
                PendingInstallments = pendingInstallments,
                InstallmentsFmt = pendingInstallments.ToString("C", culture),
                HasCreditCardPayment = hasCreditCardPayment
            });
        }

        return result;
    }
}