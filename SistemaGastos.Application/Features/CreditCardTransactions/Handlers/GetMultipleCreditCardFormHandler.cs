using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Transactions.Queries;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Enums;

namespace SistemaGastos.Application.Features.Transactions.Handlers;

public class GetMultipleCreditCardFormHandler(IApplicationDbContext context)
    : IRequestHandler<GetMultipleCreditCardFormQuery, CreditCardFormDto>
{
    public async Task<CreditCardFormDto> Handle(GetMultipleCreditCardFormQuery request, CancellationToken cancellationToken)
    {
        var accounts = await context.Account
            .AsNoTracking()
            .Where(a => a.UserID == request.UserId && a.Type == AccountType.TarjetaCredito)
            .OrderBy(a => a.Currency).ThenBy(a => a.Name)
            .Select(a => new CreditCardAccountLookupDto(a.ID, a.Name, a.Currency))
            .ToListAsync(cancellationToken);

        var categories = await context.Category
            .AsNoTracking()
            .Where(c => c.Type == "Gasto")
            .OrderBy(c => c.Name)
            .Select(c => new CreditCardFormCategoryDto(c.ID, c.Name))
            .ToListAsync(cancellationToken);

        return new CreditCardFormDto([], accounts, categories, null);
    }
}
