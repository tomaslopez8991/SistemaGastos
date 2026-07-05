using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Transactions.Queries;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Enums;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.Transactions.Handlers;

public class GetCreditCardTransactionFormHandler(IApplicationDbContext context)
    : IRequestHandler<GetCreditCardTransactionFormQuery, CreditCardFormDto>
{
    public async Task<CreditCardFormDto> Handle(GetCreditCardTransactionFormQuery request, CancellationToken cancellationToken)
    {
        var creditCards = await context.Account
            .AsNoTracking()
            .Where(a => a.UserID == request.UserId && a.Type == AccountType.TarjetaCredito)
            .OrderBy(a => a.Name)
            .Select(a => new CreditCardAccountLookupDto(a.ID, a.Name, a.Currency))
            .ToListAsync(cancellationToken);

        var accounts = await context.Account
            .AsNoTracking()
            .Where(a => a.UserID == request.UserId && a.Type == AccountType.TarjetaCredito)
            .OrderBy(a => a.Currency).ThenBy(a => a.Name)
            .Select(a => new CreditCardAccountLookupDto(a.ID, a.Name, a.Currency))
            .ToListAsync(cancellationToken);

        var categories = await context.Category
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CreditCardFormCategoryDto(c.ID, c.Name))
            .ToListAsync(cancellationToken);

        Domain.Models.CreditCardTransaction? transaction = null;
        if (request.Id.HasValue)
            transaction = await context.CreditCardTransaction
                .Include(t => t.SharedWith).ThenInclude(s => s.Person)
                .FirstOrDefaultAsync(t => t.ID == request.Id.Value, cancellationToken);

        var persons = await context.Person
            .AsNoTracking()
            .Where(p => p.UserID == request.UserId && p.Active)
            .OrderBy(p => p.Name)
            .Select(p => new PersonDropdownDto { ID = p.ID, Name = p.Name })
            .ToListAsync(cancellationToken);

        return new CreditCardFormDto(creditCards, accounts, categories, transaction, persons);
    }
}
