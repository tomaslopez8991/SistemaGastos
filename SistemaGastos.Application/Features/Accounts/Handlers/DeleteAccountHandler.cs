using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.Accounts.Commands;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.Accounts.Handlers;

public class DeleteAccountHandler(IApplicationDbContext context, ICurrentUserService user)
    : IRequestHandler<DeleteAccountCommand, bool>
{
    public async Task<bool> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        if (user.UserId == null) return false;

        // 1. Buscar la cuenta
        var entity = await context.Account
            .Include(a => a.Transactions) // Incluir relaciones si necesitas verificar dependencias
            .Include(a => a.CreditCardTransactions)
            .FirstOrDefaultAsync(a => a.ID == request.Id && a.Login.ID == user.UserId, cancellationToken);

        if (entity == null) return false;

        // 2. Validación de Seguridad (Opcional pero recomendada):
        // Si tiene movimientos, quizás no deberías borrarla, o deberías borrar en cascada.
        // Por ahora, asumimos borrado simple o que la BD tiene "OnDelete: Cascade".
        if (entity.Transactions.Any() || entity.CreditCardTransactions.Any())
        {
            throw new Exception("No se puede eliminar una cuenta con movimientos");
        }

        // 3. Eliminar
        context.Account.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}