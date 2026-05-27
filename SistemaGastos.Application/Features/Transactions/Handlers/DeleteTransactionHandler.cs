using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.Transactions.Commands;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.Transactions.Handlers;

public class DeleteTransactionHandler(IApplicationDbContext context)
    : IRequestHandler<DeleteTransactionCommand, bool>
{
    public async Task<bool> Handle(DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        using var dbTransaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var transaction = await context.Transaction
                .Include(t => t.Account)
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.ID == request.ID && t.Account.UserID == request.UserID, cancellationToken);

            if (transaction == null)
                throw new KeyNotFoundException("Transacción no encontrada");

            var account = transaction.Account;
            if (transaction.Category.Type == "Ingreso")
                account.Balance -= transaction.Amount;
            else
                account.Balance += transaction.Amount;

            context.Transaction.Remove(transaction);
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
