using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.FixedIncome.Commands;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.FixedIncome.Handlers;

public class ToggleFixedIncomeActiveHandler(IApplicationDbContext context)
    : IRequestHandler<ToggleFixedIncomeActiveCommand, bool>
{
    public async Task<bool> Handle(ToggleFixedIncomeActiveCommand request, CancellationToken cancellationToken)
    {
        var income = await context.FixedIncome
            .FirstOrDefaultAsync(f => f.ID == request.ID && f.UserID == request.UserID, cancellationToken);

        if (income is null) return false;

        income.Active = !income.Active;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
