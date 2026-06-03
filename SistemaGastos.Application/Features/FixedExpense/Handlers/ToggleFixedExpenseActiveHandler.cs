using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.FixedExpense.Commands;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.FixedExpense.Handlers;

public class ToggleFixedExpenseActiveHandler(IApplicationDbContext context)
    : IRequestHandler<ToggleFixedExpenseActiveCommand, bool>
{
    public async Task<bool> Handle(ToggleFixedExpenseActiveCommand request, CancellationToken cancellationToken)
    {
        var expense = await context.FixedExpense
            .FirstOrDefaultAsync(f => f.ID == request.ID && f.UserID == request.UserID, cancellationToken);

        if (expense == null)
            throw new Exception("Gasto fijo no encontrado");

        if (expense.Active)
        {
            // PAUSAR: solo desactivar, sin tocar StartDate
            expense.Active = false;
        }
        else
        {
            // REANUDAR: activar y ajustar StartDate al mes indicado
            expense.Active = true;
            expense.StartDate = request.ActivateFromDate.HasValue
                ? new DateTime(request.ActivateFromDate.Value.Year, request.ActivateFromDate.Value.Month, 1)
                : null;
        }

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
