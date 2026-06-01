using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.FixedIncome.Commands;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.FixedIncome.Handlers;

public class DeleteFixedIncomeHandler(IApplicationDbContext context)
    : IRequestHandler<DeleteFixedIncomeCommand, bool>
{
    public async Task<bool> Handle(DeleteFixedIncomeCommand request, CancellationToken cancellationToken)
    {
        var income = await context.FixedIncome
            .FirstOrDefaultAsync(f => f.ID == request.ID && f.UserID == request.UserID, cancellationToken);

        if (income is null) return false;

        context.FixedIncome.Remove(income);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
