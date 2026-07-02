using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.Transactions.Queries;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Application.ViewModels;

namespace SistemaGastos.Application.Features.Transactions.Handlers;

public class GetCreditCardIndexHandler(IApplicationDbContext context)
    : IRequestHandler<GetCreditCardIndexQuery, CreditCardTransactionIndexVM>
{
    public async Task<CreditCardTransactionIndexVM> Handle(GetCreditCardIndexQuery request, CancellationToken cancellationToken)
    {
        var accounts = await context.Account
            .AsNoTracking()
            .Where(a => a.UserID == request.UserId)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

        var categories = await context.Category
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return new CreditCardTransactionIndexVM { Accounts = accounts, Categories = categories };
    }
}
