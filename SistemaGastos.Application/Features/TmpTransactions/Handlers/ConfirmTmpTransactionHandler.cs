using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.TmpTransactions.Commands;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.TmpTransactions.Handlers;

public class ConfirmTmpTransactionHandler(IApplicationDbContext context)
    : IRequestHandler<ConfirmTmpTransactionCommand, bool>
{
    public async Task<bool> Handle(ConfirmTmpTransactionCommand request, CancellationToken cancellationToken)
    {
        // 1. Buscar la transacción temporal
        var tmpTransaction = await context.TmpTransaction
            .Include(t => t.Account)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.ID == request.ID
                && t.Account.UserID == request.UserID, cancellationToken);

        if (tmpTransaction == null)
            throw new Exception("Transacción temporal no encontrada");

        // 2. Validar que la cuenta pertenezca al usuario
        var account = await context.Account
            .FirstOrDefaultAsync(a => a.ID == tmpTransaction.AccountID
                && a.UserID == request.UserID, cancellationToken);

        if (account == null)
            throw new Exception("Cuenta no encontrada o no pertenece al usuario");

        // 3. Validar que la categoría exista
        var category = await context.Category
            .FirstOrDefaultAsync(c => c.ID == tmpTransaction.CategoryID, cancellationToken);

        if (category == null)
            throw new Exception("Categoría no encontrada");

        var transaction = new Transaction
        {
            Date = (DateTime)tmpTransaction.DateTransaction,
            Amount = tmpTransaction.Amount,
            Description = tmpTransaction.Description,
            AccountID = tmpTransaction.AccountID,
            CategoryID = tmpTransaction.CategoryID
        };

        // 5. Actualizar saldo de la cuenta
        if (category.Type == "Ingreso")
        {
            account.Balance += tmpTransaction.Amount;
        }
        else if (category.Type == "Gasto")
        {
            account.Balance -= tmpTransaction.Amount;
        }

        // 6. Agregar la transacción real
        context.Transaction.Add(transaction);

        // 7. Eliminar la transacción temporal
        context.TmpTransaction.Remove(tmpTransaction);

        // 8. Guardar cambios
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex)
        {
            // Log detallado del error
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            throw new Exception($"Error al guardar la transacción: {innerMessage}", ex);
        }
    }
}