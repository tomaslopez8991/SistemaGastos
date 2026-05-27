using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.Transactions.Queries;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.Transactions.Handlers;

public class DeleteTransactionsHandler(IApplicationDbContext context)
    : IRequestHandler<DeleteTransactionsCommand, bool>
{
    public async Task<bool> Handle(DeleteTransactionsCommand request, CancellationToken cancellationToken)
    {
        if (request.Ids is []) return false;

        using var dbTransaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var transactions = await context.CreditCardTransaction
                .Include(t => t.Account)
                .Include(t => t.Category)
                .Where(t => request.Ids.Contains(t.ID))
                .ToListAsync(cancellationToken);

            if (transactions.Count == 0) return false;

            foreach (var t in transactions)
            {
                // Revertir Saldos (Lógica Inversa al Create)
                if (t.Account != null && t.Category != null)
                {
                    if (t.Category.Type == "Gasto") t.Account.Balance += t.Amount;
                    else if (t.Category.Type == "Ingreso") t.Account.Balance -= t.Amount;
                }
            }

            context.CreditCardTransaction.RemoveRange(transactions);
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