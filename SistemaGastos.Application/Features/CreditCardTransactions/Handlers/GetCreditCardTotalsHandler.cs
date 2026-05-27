using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Transactions.Queries;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.Transactions.Handlers;

public class GetCreditCardTotalsHandler(IApplicationDbContext context)
    : IRequestHandler<GetCreditCardTotalsQuery, CreditCardTotalsDto>
{
    public async Task<CreditCardTotalsDto> Handle(GetCreditCardTotalsQuery request, CancellationToken cancellationToken)
    {
        var totals = await context.CreditCardTransaction
            .AsNoTracking()
            .Where(t => t.Account.UserID == request.UserId)
            .GroupBy(t => new { t.Fixed, t.Account.Currency })
            .Select(g => new { g.Key.Fixed, g.Key.Currency, Total = g.Sum(t => t.Amount) })
            .ToListAsync(cancellationToken);

        decimal Get(bool isFixed, string currency) =>
            totals.FirstOrDefault(x => x.Fixed == isFixed && x.Currency == currency)?.Total ?? 0;

        return new CreditCardTotalsDto(
            TotalArs: Get(false, "ARS") + Get(true, "ARS"),
            TotalUsd: Get(false, "USD") + Get(true, "USD"),
            FixedArs: Get(true, "ARS"),
            FixedUsd: Get(true, "USD"),
            VariableArs: Get(false, "ARS"),
            VariableUsd: Get(false, "USD")
        );
    }
}
