using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.TmpTransactions.Commands;
using SistemaGastos.Application.Interfaces;
using System.Text.Json;

namespace SistemaGastos.Application.Features.TmpTransactions.Handlers;

public class SetDayAmountOverrideHandler(IApplicationDbContext context)
    : IRequestHandler<SetDayAmountOverrideCommand, bool>
{
    public async Task<bool> Handle(SetDayAmountOverrideCommand request, CancellationToken cancellationToken)
    {
        var tx = await context.TmpTransaction
            .FirstOrDefaultAsync(t => t.ID == request.TmpTransactionID && t.UserID == request.UserID, cancellationToken)
            ?? throw new Exception("Transacción planificada no encontrada");

        var overrides = string.IsNullOrEmpty(tx.DayAmountOverrides)
            ? new Dictionary<string, decimal>()
            : JsonSerializer.Deserialize<Dictionary<string, decimal>>(tx.DayAmountOverrides) ?? new();

        var dayKey = request.Day.ToString();
        if (request.Amount.HasValue && request.Amount.Value > 0)
            overrides[dayKey] = request.Amount.Value;
        else
            overrides.Remove(dayKey);

        tx.DayAmountOverrides = overrides.Count > 0
            ? JsonSerializer.Serialize(overrides)
            : null;

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
