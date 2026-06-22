using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.FixedIncome.Commands;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.FixedIncome.Handlers;

public class ToggleFixedIncomePauseHandler(IApplicationDbContext context)
    : IRequestHandler<ToggleFixedIncomePauseCommand, bool>
{
    public async Task<bool> Handle(ToggleFixedIncomePauseCommand request, CancellationToken cancellationToken)
    {
        var income = await context.FixedIncome
            .FirstOrDefaultAsync(f => f.ID == request.ID && f.UserID == request.UserID, cancellationToken)
            ?? throw new Exception("Ingreso fijo no encontrado");

        var key = $"{request.Year}-{request.Month:D2}";
        var paused = string.IsNullOrEmpty(income.PausedMonths)
            ? new List<string>()
            : income.PausedMonths.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

        if (paused.Contains(key))
            paused.Remove(key);
        else
            paused.Add(key);

        income.PausedMonths = paused.Count > 0 ? string.Join(',', paused) : null;

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
