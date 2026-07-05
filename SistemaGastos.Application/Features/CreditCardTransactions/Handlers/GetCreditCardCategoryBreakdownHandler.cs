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
        var txList = await context.CreditCardTransaction
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Where(t => t.Account.UserID == request.UserId)
            .ToListAsync(cancellationToken);

        var rows = txList
            .GroupBy(t => new { Name = t.Category?.Name ?? "", Currency = t.Account?.Currency ?? "ARS" })
            .Select(g => new
            {
                g.Key.Name,
                g.Key.Currency,
                Total = g.Sum(t => t.Amount),
                Count = g.Count()
            })
            .ToList();

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
