using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Transactions.Commands;
using SistemaGastos.Application.Interfaces;
using TransactionEntity = SistemaGastos.Domain.Models.Transaction;

namespace SistemaGastos.Application.Features.Transactions.Handlers;

public class CreateTransactionHandler(IApplicationDbContext context)
    : IRequestHandler<CreateTransactionCommand, int>
{
    public async Task<int> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        if (request.Splits == null || !request.Splits.Any())
        {
            request.Splits = new List<TransactionSplitDto>
        {
            new TransactionSplitDto { AccountID = request.AccountID, Amount = request.Amount }
        };
        }

        using var dbTransaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var category = await context.Category
                .FirstOrDefaultAsync(c => c.ID == request.CategoryID, cancellationToken);

            if (category == null)
                throw new InvalidOperationException("Categoría no encontrada");

            int firstTransactionId = 0;

            foreach (var split in request.Splits)
            {
                var account = await context.Account
                    .FirstOrDefaultAsync(a => a.ID == split.AccountID && a.UserID == request.UserID, cancellationToken);

                if (account == null)
                    throw new InvalidOperationException($"Cuenta no encontrada o no autorizada (ID: {split.AccountID})");

                var transaction = new TransactionEntity
                {
                    Date = request.Date,
                    Amount = split.Amount,
                    AccountID = split.AccountID,
                    CategoryID = request.CategoryID,
                    Description = request.Description,
                    FixedExpenseID = request.FixedExpenseID,
                    PersonID = request.PersonID
                };

                await context.Transaction.AddAsync(transaction, cancellationToken);

                if (category.Type == "Ingreso")
                    account.Balance += split.Amount;
                else
                    account.Balance -= split.Amount;

                await context.SaveChangesAsync(cancellationToken);

                if (firstTransactionId == 0) firstTransactionId = transaction.ID;
            }

            await dbTransaction.CommitAsync(cancellationToken);

            return firstTransactionId;
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
