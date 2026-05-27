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

        expense.Active = !expense.Active;

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
