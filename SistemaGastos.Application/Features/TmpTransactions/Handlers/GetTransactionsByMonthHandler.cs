using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.TmpTransactions.Queries;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.TmpTransactions.Handlers;

public class GetTransactionsByMonthHandler(IApplicationDbContext context)
    : IRequestHandler<GetTransactionsByMonthQuery, List<TmpTransactionDto>>
{
    public async Task<List<TmpTransactionDto>> Handle(GetTransactionsByMonthQuery request, CancellationToken cancellationToken)
    {
        var paidFixedExpenseIds = await context.Transaction
            .AsNoTracking()
            .Where(t => t.Account.UserID == request.UserID
                        && t.Date.Year == request.Year
                        && t.Date.Month == request.Month
                        && t.FixedExpenseID != null)
            .Select(t => t.FixedExpenseID)
            .ToListAsync(cancellationToken);

        var transactions = await context.TmpTransaction
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Where(t => t.UserID == request.UserID
                        && t.DateTransaction.HasValue
                        && t.DateTransaction.Value.Year == request.Year
                        && t.DateTransaction.Value.Month == request.Month)
            .ToListAsync(cancellationToken);

        var culture = new System.Globalization.CultureInfo("es-AR");

        return transactions
            .OrderByDescending(t => t.Category.Type == "Ingreso" ? t.Amount : -t.Amount)
            .Select(t =>
            {
                var sign = t.Category.Type == "Ingreso" ? "+ " : "- ";
                var amountFmt = t.Currency == "USD"
                    ? $"{sign}USD {t.Amount:N2}"
                    : sign + t.Amount.ToString("C", culture);

                return new TmpTransactionDto
                {
                    ID = t.ID,
                    Description = t.Description,
                    Amount = t.Amount,
                    Currency = t.Currency,
                    CategoryID = t.CategoryID,
                    CategoryType = t.Category.Type,
                    AmountFormatted = amountFmt,
                    IsIngreso = t.Category.Type == "Ingreso",
                };
            })
            .ToList();
    }
}
