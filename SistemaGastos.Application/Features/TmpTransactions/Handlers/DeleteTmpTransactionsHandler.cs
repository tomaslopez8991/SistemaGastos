using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.TmpTransactions.Commands;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.TmpTransactions.Handlers;

public class DeleteTmpTransactionsHandler(IApplicationDbContext context)
    : IRequestHandler<DeleteTmpTransactionsCommand, int>
{
    public async Task<int> Handle(DeleteTmpTransactionsCommand request, CancellationToken cancellationToken)
    {
        var transactions = await context.TmpTransaction
            .Where(t => request.IDs.Contains(t.ID) && t.UserID == request.UserID)
            .ToListAsync(cancellationToken);

        if (!transactions.Any())
            return 0;

        context.TmpTransaction.RemoveRange(transactions);
        return await context.SaveChangesAsync(cancellationToken);
    }
}
