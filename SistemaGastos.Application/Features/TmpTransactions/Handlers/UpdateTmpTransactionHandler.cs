using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.TmpTransactions.Commands;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.TmpTransactions.Handlers;

public class UpdateTmpTransactionHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateTmpTransactionCommand, bool>
{
    public async Task<bool> Handle(UpdateTmpTransactionCommand request, CancellationToken cancellationToken)
    {
        var existing = await context.TmpTransaction
            .FirstOrDefaultAsync(t => t.ID == request.ID && t.UserID == request.UserID, cancellationToken);

        if (existing == null)
            return false;

        existing.Description = request.Description;
        existing.Amount = request.Amount;
        existing.Currency = request.Currency;
        existing.CategoryID = request.CategoryID;
        existing.AccountID = (int)request.AccountID;
        existing.DateTransaction = request.DateTransaction;
        existing.DistributionEndDay = request.DistributionEndDay;
        existing.ExcludedDays = request.ExcludedDays;

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
