using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.TmpTransactions.Commands;
using SistemaGastos.Application.Helpers;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Models;
using System.Text.Json;

namespace SistemaGastos.Application.Features.TmpTransactions.Handlers;

public class ConfirmTmpTransactionDayHandler(IApplicationDbContext context)
    : IRequestHandler<ConfirmTmpTransactionDayCommand, bool>
{
    public async Task<bool> Handle(ConfirmTmpTransactionDayCommand request, CancellationToken cancellationToken)
    {
        // 1. Buscar la transacción temporal
        var tmp = await context.TmpTransaction
            .Include(t => t.Account)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.ID == request.ID
                && t.Account.UserID == request.UserID, cancellationToken);

        if (tmp == null)
            throw new Exception("Transacción temporal no encontrada");

        // 2. Validar que la cuenta pertenezca al usuario
        var account = await context.Account
            .FirstOrDefaultAsync(a => a.ID == tmp.AccountID
                && a.UserID == request.UserID, cancellationToken);

        if (account == null)
            throw new Exception("Cuenta no encontrada o no pertenece al usuario");

        // 3. Validar que la categoría exista
        var category = await context.Category
            .FirstOrDefaultAsync(c => c.ID == tmp.CategoryID, cancellationToken);

        if (category == null)
            throw new Exception("Categoría no encontrada");

        // 4. Calcular el monto correspondiente al día solicitado
        var dateTransaction = tmp.DateTransaction!.Value;
        var daysInMonth = DateTime.DaysInMonth(dateTransaction.Year, dateTransaction.Month);
        var excluded = DistributionHelper.ParseExcludedDays(tmp.ExcludedDays);

        var distribution = DistributionHelper.Distribute(tmp.Amount, dateTransaction.Day, tmp.DistributionEndDay, excluded, daysInMonth);
        if (!distribution.TryGetValue(request.Day, out var portionAmount))
            throw new Exception("El día indicado no forma parte de la distribución");

        // Aplicar override de monto si el usuario lo modificó manualmente para este día
        var overrides = string.IsNullOrEmpty(tmp.DayAmountOverrides)
            ? new Dictionary<string, decimal>()
            : JsonSerializer.Deserialize<Dictionary<string, decimal>>(tmp.DayAmountOverrides) ?? new();

        if (overrides.TryGetValue(request.Day.ToString(), out var overriddenAmount))
            portionAmount = overriddenAmount;

        // 5. Registrar la transacción real para ese día
        var transaction = new Transaction
        {
            Date = new DateTime(dateTransaction.Year, dateTransaction.Month, request.Day),
            Amount = portionAmount,
            Description = tmp.Description,
            AccountID = tmp.AccountID,
            CategoryID = tmp.CategoryID
        };

        // 6. Actualizar saldo de la cuenta
        if (category.Type == "Ingreso")
        {
            account.Balance += portionAmount;
        }
        else if (category.Type == "Gasto")
        {
            account.Balance -= portionAmount;
        }

        context.Transaction.Add(transaction);

        // 7. Excluir el día confirmado, limpiar su override y redistribuir el monto restante
        excluded.Add(request.Day);
        overrides.Remove(request.Day.ToString());
        tmp.DayAmountOverrides = overrides.Count > 0 ? JsonSerializer.Serialize(overrides) : null;

        var remainingAmount = tmp.Amount - portionAmount;
        var remaining = DistributionHelper.Distribute(remainingAmount, dateTransaction.Day, tmp.DistributionEndDay, excluded, daysInMonth);

        if (remaining.Count == 0 || remainingAmount <= 0)
        {
            context.TmpTransaction.Remove(tmp);
        }
        else
        {
            tmp.Amount = remainingAmount;
            tmp.ExcludedDays = DistributionHelper.SerializeExcludedDays(excluded);
        }

        // 8. Guardar cambios
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            throw new Exception($"Error al guardar la transacción: {innerMessage}", ex);
        }
    }
}
