using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.FixedExpense.Commands;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.FixedExpense.Handlers;

public class UpdateFixedExpenseHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateFixedExpenseCommand, bool>
{
    public async Task<bool> Handle(UpdateFixedExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = await context.FixedExpense
            .FirstOrDefaultAsync(x => x.ID == request.ID && x.UserID == request.UserID, cancellationToken);

        if (expense == null)
            return false;

        // ✅ AUDITORÍA: Detectar cambio de precio
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
        expense.Currency = request.Currency;
        expense.AccountID = request.AccountID;
        expense.CategoryID = request.CategoryID;
        expense.PaymentDay = request.PaymentDay;
        expense.LogoUrl = request.LogoUrl;
        expense.PersonID = request.PersonID;
        expense.PersonPercentage = request.PersonID.HasValue ? (request.PersonPercentage ?? 100m) : null;
        expense.StartDate = request.StartDate;
        expense.DistributionEndDay = request.DistributionEndDay;
        expense.ExcludedDays = request.ExcludedDays;

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
