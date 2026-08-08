using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.FixedExpense.Commands;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Application.Helpers;

namespace SistemaGastos.Application.Features.FixedExpense.Handlers;

public class DeleteFixedExpenseHandler(IApplicationDbContext context)
    : IRequestHandler<DeleteFixedExpenseCommand, bool>
{
    public async Task<bool> Handle(DeleteFixedExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = await context.FixedExpense
            .FirstOrDefaultAsync(f => f.ID == request.ID && f.UserID == request.UserID, cancellationToken);

        if (expense == null)
            throw new Exception("Gasto fijo no encontrado");

        if (InterestExpenseHelper.IsLiveAccrual(expense, DateTime.Today))
            throw new InvalidOperationException("El interés del mes en curso todavía se calcula diariamente y no puede eliminarse.");

        context.FixedExpense.Remove(expense);

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
