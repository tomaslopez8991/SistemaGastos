using MediatR;
using SistemaGastos.Application.Features.Budgets.Commands;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.Budgets.Handlers;

public class DeleteBudgetHandler(IApplicationDbContext context)
    : IRequestHandler<DeleteBudgetCommand, bool>
{
    public async Task<bool> Handle(DeleteBudgetCommand request, CancellationToken cancellationToken)
    {
        var budget = await context.Budget.FindAsync([request.Id], cancellationToken);
        if (budget == null) return true;

        context.Budget.Remove(budget);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
