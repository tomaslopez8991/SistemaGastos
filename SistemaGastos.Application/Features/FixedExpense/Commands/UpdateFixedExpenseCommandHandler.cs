using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.FixedExpense.Commands;

public class UpdateFixedExpenseCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateFixedExpenseCommand, bool>
{
    public async Task<bool> Handle(UpdateFixedExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = await context.FixedExpense
            .FirstOrDefaultAsync(x => x.ID == request.ID, cancellationToken);

        if (expense == null)
            return false;

        // Auditoría de cambio de precio
        if (expense.Amount != request.Amount)
        {
            var historyEntry = new FixedExpenseHistory
            {
                FixedExpenseID = expense.ID,
                OldAmount = expense.Amount,
                NewAmount = request.Amount,
                ChangeDate = DateTime.UtcNow
            };

            await context.FixedExpenseHistory.AddAsync(historyEntry, cancellationToken);
        }

        // Actualizar campos
        expense.Name = request.Name;
        expense.Amount = request.Amount;
        expense.AccountID = request.AccountID;
        expense.CategoryID = (int)request.CategoryID;
        expense.LastGeneratedDate = DateTime.UtcNow;
        expense.DistributionEndDay = request.DistributionEndDay;
        expense.ExcludedDays = request.ExcludedDays;

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
