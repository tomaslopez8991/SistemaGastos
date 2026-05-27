using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.TmpTransactions.Queries;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.TmpTransactions.Handlers;

public class GetDashboardStatsHandler(IApplicationDbContext context, IMapper mapper)
    : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var user = await context.Login
            .FirstOrDefaultAsync(l => l.ID == request.UserID, cancellationToken);

        // Calcular saldo total ARS
        var saldoTotalARS = await context.Account
            .Where(a => a.Currency == "ARS" && a.UserID == request.UserID)
            .SumAsync(a => (decimal?)a.Balance, cancellationToken) ?? 0m;

        // Obtener transacciones temporales con categorías
        var transactions = await context.TmpTransaction
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Where(t => t.UserID == request.UserID && t.DateTransaction.HasValue)
            .ToListAsync(cancellationToken);

        var transactionsDto = transactions
            .OrderByDescending(t => t.Category.Type == "Ingreso" ? t.Amount : -t.Amount)
            .Select(t => new TmpTransactionDto
            {
                ID = t.ID,
                Description = t.Description,
                Amount = t.Amount,
                CategoryID = t.CategoryID,
                CategoryName = t.Category.Name,
                CategoryType = t.Category.Type,
                AccountID = t.AccountID,
                AccountName = t.Account?.Name,
                DateTransaction = t.DateTransaction,
                IsIngreso = t.Category.Type == "Ingreso",
                AmountFormatted = (t.Category.Type == "Ingreso" ? "+ " : "- ") +
                                  t.Amount.ToString("C", new System.Globalization.CultureInfo("es-AR"))
            })
            .ToList();

        // Calcular balances mensuales
        var startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var months = Enumerable.Range(0, 12)
            .Select(i => startDate.AddMonths(i))
            .ToList();

        var monthlyBalances = new Dictionary<string, decimal>();
        decimal saldoAcumulado = saldoTotalARS;

        foreach (var m in months)
        {
            var key = $"{m.Year}-{m.Month:D2}";

            var ingresosMes = transactions
                .Where(t => t.Category.Type == "Ingreso"
                            && t.DateTransaction.Value.Month == m.Month
                            && t.DateTransaction.Value.Year == m.Year)
                .Sum(t => t.Amount);

            var gastosMes = transactions
                .Where(t => t.Category.Type == "Gasto"
                            && t.DateTransaction.Value.Month == m.Month
                            && t.DateTransaction.Value.Year == m.Year)
                .Sum(t => t.Amount);

            saldoAcumulado += ingresosMes - gastosMes;
            monthlyBalances[key] = saldoAcumulado;
        }

        return new DashboardStatsDto
        {
            TotalARSValue = saldoTotalARS,
            MonthlyBalances = monthlyBalances,
            Months = months,
            Transactions = transactionsDto
        };
    }
}
