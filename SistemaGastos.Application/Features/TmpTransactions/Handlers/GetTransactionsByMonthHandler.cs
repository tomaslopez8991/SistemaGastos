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

        return transactions
            .OrderByDescending(t => t.Category.Type == "Ingreso" ? t.Amount : -t.Amount)
            .Select(t => new TmpTransactionDto
            {
                ID = t.ID,
                Description = t.Description,
                Amount = t.Amount,
                CategoryID = t.CategoryID,
                CategoryType = t.Category.Type,
                AmountFormatted = (t.Category.Type == "Ingreso" ? "+ " : "- ") +
                                  t.Amount.ToString("C", new System.Globalization.CultureInfo("es-AR")),
                IsIngreso = t.Category.Type == "Ingreso",

                //IsPaid = t.FixedExpenseID != null && paidFixedExpenseIds.Contains(t.FixedExpenseID)
            })
            .ToList();
    }
}
