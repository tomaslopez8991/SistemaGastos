using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.FixedExpense.Commands;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.FixedExpense.Handlers;

public class ToggleFixedExpensePauseHandler(IApplicationDbContext context)
    : IRequestHandler<ToggleFixedExpensePauseCommand, bool>
{
    public async Task<bool> Handle(ToggleFixedExpensePauseCommand request, CancellationToken cancellationToken)
    {
        var expense = await context.FixedExpense
            .FirstOrDefaultAsync(f => f.ID == request.ID && f.UserID == request.UserID, cancellationToken)
            ?? throw new Exception("Gasto fijo no encontrado");

        var key = $"{request.Year}-{request.Month:D2}";
        var paused = string.IsNullOrEmpty(expense.PausedMonths)
            ? new List<string>()
            : expense.PausedMonths.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

        if (paused.Contains(key))
            paused.Remove(key);
        else
            paused.Add(key);

        expense.PausedMonths = paused.Count > 0 ? string.Join(',', paused) : null;

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
