using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.Accounts.Commands;
using SistemaGastos.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaGastos.Application.Features.Accounts.Handlers;

public class TransferMoneyHandler(IApplicationDbContext context, ICurrentUserService user)
    : IRequestHandler<TransferMoneyCommand, bool>
{
    public async Task<bool> Handle(TransferMoneyCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        if (user.UserId == null || dto.Amount <= 0 || dto.OriginAccountId == dto.DestinationAccountId) return false;

        using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var origin = await context.Account
                .Include(a => a.Login)
                .FirstOrDefaultAsync(a => a.ID == dto.OriginAccountId, cancellationToken);
            var dest = await context.Account
                .Include(a => a.Login)
                .FirstOrDefaultAsync(a => a.ID == dto.DestinationAccountId, cancellationToken);

            // Validaciones de seguridad
            if (origin == null || dest == null) throw new Exception("Cuentas no encontradas");
            if (origin.Login.ID != user.UserId || dest.Login.ID != user.UserId) throw new Exception("Acceso denegado");
            if (origin.Currency != dest.Currency) throw new Exception("Monedas distintas");

            // Ejecución (Permitimos saldo negativo según tu requisito)
            origin.Balance -= dto.Amount;
            dest.Balance += dto.Amount;

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}