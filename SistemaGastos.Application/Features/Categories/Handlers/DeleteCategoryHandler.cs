using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.Categories.Commands;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.Categories.Handlers;

public class DeleteCategoryHandler(IApplicationDbContext context)
    : IRequestHandler<DeleteCategoryCommand, bool>
{
    public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await context.Category.FindAsync([request.Id], cancellationToken);
        if (category == null)
        {
            throw new KeyNotFoundException("Categoría no encontrada.");
        }

        var isInUse = await context.Transaction.AnyAsync(x => x.CategoryID == request.Id, cancellationToken)
            || await context.TmpTransaction.AnyAsync(x => x.CategoryID == request.Id, cancellationToken)
            || await context.CreditCardTransaction.AnyAsync(x => x.CategoryID == request.Id, cancellationToken)
            || await context.FixedExpense.AnyAsync(x => x.CategoryID == request.Id, cancellationToken)
            || await context.FixedIncome.AnyAsync(x => x.CategoryID == request.Id, cancellationToken)
            || await context.Budget.AnyAsync(x => x.CategoryID == request.Id, cancellationToken);

        if (isInUse)
        {
            throw new InvalidOperationException(
                "No se puede eliminar porque está utilizada en movimientos, planificaciones o presupuestos. Podés editarla para conservar el historial.");
        }

        context.Category.Remove(category);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
