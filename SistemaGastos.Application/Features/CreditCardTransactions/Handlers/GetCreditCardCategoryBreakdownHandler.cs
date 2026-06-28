using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Transactions.Queries;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.Transactions.Handlers;

public class GetCreditCardCategoryBreakdownHandler(IApplicationDbContext context)
    : IRequestHandler<GetCreditCardCategoryBreakdownQuery, List<CategoryBreakdownDto>>
{
    public async Task<List<CategoryBreakdownDto>> Handle(GetCreditCardCategoryBreakdownQuery request, CancellationToken cancellationToken)
    {
        var rows = await context.CreditCardTransaction
            .AsNoTracking()
            .Where(t => t.Account.UserID == request.UserId)
            .GroupBy(t => new { t.Category.Name, t.Account.Currency })
            .Select(g => new
            {
                g.Key.Name,
                g.Key.Currency,
                Total = g.Sum(t => t.PersonID != null && t.PersonPercentage != null
                    ? t.Amount * (t.PersonPercentage.Value / 100m)
                    : t.Amount),
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.Name)
            .Select(g => new CategoryBreakdownDto(
                CategoryName: g.Key,
                TotalArs: g.Where(x => x.Currency == "ARS").Sum(x => x.Total),
                TotalUsd: g.Where(x => x.Currency == "USD").Sum(x => x.Total),
                Count: g.Sum(x => x.Count)
            ))
            .OrderByDescending(x => x.TotalArs + x.TotalUsd)
            .ToList();
    }
}
