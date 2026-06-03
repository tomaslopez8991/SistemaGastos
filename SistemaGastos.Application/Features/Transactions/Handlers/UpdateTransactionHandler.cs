using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Transactions.Commands;
using SistemaGastos.Application.Interfaces;
using TransactionEntity = SistemaGastos.Domain.Models.Transaction;

namespace SistemaGastos.Application.Features.Transactions.Handlers;

public class UpdateTransactionHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateTransactionCommand, bool>
{
    public async Task<bool> Handle(UpdateTransactionCommand request, CancellationToken cancellationToken)
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
            var transaction = await context.Transaction
                .Include(t => t.Account)
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.ID == request.ID && t.Account.UserID == request.UserID, cancellationToken);

            if (transaction == null)
                throw new KeyNotFoundException("Transacción no encontrada");

            var oldAmount = transaction.Amount;
            var oldCategoryType = transaction.Category.Type;

            var oldAccount = await context.Account.FindAsync(new object[] { transaction.AccountID }, cancellationToken);
            if (oldAccount != null)
            {
                if (oldCategoryType == "Ingreso")
                    oldAccount.Balance -= oldAmount;
                else
                    oldAccount.Balance += oldAmount;
            }

            var newCategory = await context.Category
                .FirstOrDefaultAsync(c => c.ID == request.CategoryID, cancellationToken);

            if (newCategory == null)
                throw new InvalidOperationException("Categoría no encontrada");

            bool isFirstSplit = true;

            foreach (var split in request.Splits)
            {
                var accountToUpdate = await context.Account
                    .FirstOrDefaultAsync(a => a.ID == split.AccountID && a.UserID == request.UserID, cancellationToken);

                if (accountToUpdate == null)
                    throw new InvalidOperationException($"Cuenta no encontrada o no pertenece al usuario (ID: {split.AccountID})");

                if (isFirstSplit)
                {
                    transaction.Date = request.Date;
                    transaction.Amount = split.Amount;
                    transaction.AccountID = split.AccountID;
                    transaction.CategoryID = request.CategoryID;
                    transaction.Description = request.Description;
                    transaction.FixedExpenseID = request.FixedExpenseID;
                    transaction.PersonID = request.PersonID;
                    transaction.PersonPercentage = request.PersonID.HasValue ? (request.PersonPercentage ?? 100m) : null;

                    isFirstSplit = false;
                }
                else
                {
                    var newTransaction = new TransactionEntity
                    {
                        Date = request.Date,
                        Amount = split.Amount,
                        AccountID = split.AccountID,
                        CategoryID = request.CategoryID,
                        Description = request.Description,
                        FixedExpenseID = request.FixedExpenseID,
                        PersonID = request.PersonID,
                        PersonPercentage = request.PersonID.HasValue ? (request.PersonPercentage ?? 100m) : null
                    };

                    await context.Transaction.AddAsync(newTransaction, cancellationToken);
                }

                if (newCategory.Type == "Ingreso")
                    accountToUpdate.Balance += split.Amount;
                else
                    accountToUpdate.Balance -= split.Amount;
            }

            await context.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);

            return true;
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}