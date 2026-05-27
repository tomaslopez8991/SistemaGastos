using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Transactions.Queries;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.Transactions.Handlers;

public class GetTransactionFormHandler(IApplicationDbContext context)
    : IRequestHandler<GetTransactionFormQuery, TransactionFormDto>
{
    public async Task<TransactionFormDto> Handle(GetTransactionFormQuery request, CancellationToken cancellationToken)
    {
        var formDto = new TransactionFormDto
        {
            ID = 0,
            Date = DateTime.Now,
            Amount = 0,
            AccountID = 0,
            CategoryID = 0,
            Description = string.Empty,
            FixedExpenseID = null
        };

        formDto.Accounts = await context.Account
            .AsNoTracking()
            .Where(a => a.UserID == request.UserID && a.Type != Domain.Enums.AccountType.TarjetaCredito)
            .OrderBy(a => a.Currency)
            .ThenBy(a => a.Name)
            .Select(a => new AccountDto
            {
                ID = a.ID,
                Name = a.Name,
                Currency = a.Currency,
                Type = a.Type,
                Balance = a.Balance
            })
            .ToListAsync(cancellationToken);

        formDto.Categories = await context.Category
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDropdownDto
            {
                ID = c.ID,
                Name = c.Name
            })
            .ToListAsync(cancellationToken);

        formDto.FixedExpenses = await context.FixedExpense
            .AsNoTracking()
            .Where(f => f.UserID == request.UserID && f.Active)
            .OrderBy(f => f.Name)
            .Select(f => new FixedExpenseDropdownDto
            {
                ID = f.ID,
                Name = f.Name,
                Amount = f.Amount
            })
            .ToListAsync(cancellationToken);

        if (request.ID.HasValue && request.ID.Value > 0)
        {
            var transaction = await context.Transaction
                .AsNoTracking()
                .Include(t => t.Account)
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.ID == request.ID.Value && t.Account.UserID == request.UserID, cancellationToken);

            if (transaction != null)
            {
                formDto.ID = transaction.ID;
                formDto.Date = transaction.Date;
                formDto.Amount = transaction.Amount;
                formDto.AccountID = transaction.AccountID;
                formDto.CategoryID = transaction.CategoryID;
                formDto.Description = transaction.Description;
                formDto.FixedExpenseID = transaction.FixedExpenseID;
            }
        }

        return formDto;
    }
}
