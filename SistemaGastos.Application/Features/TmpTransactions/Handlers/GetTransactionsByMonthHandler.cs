using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.TmpTransactions.Queries;
using SistemaGastos.Application.Helpers;
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
        var daysInMonth = DateTime.DaysInMonth(request.Year, request.Month);

        return transactions
            .OrderByDescending(t => t.Category.Type == "Ingreso" ? t.Amount : -t.Amount)
            .SelectMany(t =>
            {
                var excluded = DistributionHelper.ParseExcludedDays(t.ExcludedDays);
                var dist = DistributionHelper.Distribute(t.Amount, t.DateTransaction!.Value.Day, t.DistributionEndDay, excluded, daysInMonth)
                    .OrderBy(kv => kv.Key)
                    .ToList();

                var sign = t.Category.Type == "Ingreso" ? "+ " : "- ";

                return dist.Select((kv, idx) =>
                {
                    var amount = kv.Value;
                    var amountFmt = t.Currency == "USD"
                        ? $"{sign}USD {amount:N2}"
                        : sign + amount.ToString("C", culture);
                    var description = dist.Count > 1 ? $"{t.Description} (día {idx + 1}/{dist.Count})" : t.Description;

                    return new TmpTransactionDto
                    {
                        ID = t.ID,
                        Description = description,
                        Amount = amount,
                        Currency = t.Currency,
                        CategoryID = t.CategoryID,
                        CategoryType = t.Category.Type,
                        AmountFormatted = amountFmt,
                        IsIngreso = t.Category.Type == "Ingreso",
                        Day = kv.Key,
                        IsDistributed = dist.Count > 1,
                        DistributionEndDay = t.DistributionEndDay,
                        ExcludedDays = t.ExcludedDays
                    };
                });
            })
            .ToList();
    }
}
