using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Transactions.Queries;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.Transactions.Handlers;

public class UpdateCreditCardTransactionHandler(IApplicationDbContext context, IMapper mapper)
    : IRequestHandler<UpdateCreditCardTransactionCommand, bool>
{
    public async Task<bool> Handle(UpdateCreditCardTransactionCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        using var dbTransaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var existing = await context.CreditCardTransaction
                .Include(t => t.Account)
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.ID == dto.ID, cancellationToken);

            if (existing == null) return false;

            // 1. REVERTIR SALDO ANTERIOR
            if (existing.Account != null && existing.Category != null)
            {
                if (existing.Category.Type == "Gasto")
                {
                    existing.Account.Balance += existing.Amount;
                }
                else if (existing.Category.Type == "Ingreso")
                {
                    existing.Account.Balance -= existing.Amount;
                }
            }

            // 2. ACTUALIZAR CAMPOS (Mapper sobrescribe la entidad con los datos nuevos)
            mapper.Map(dto, existing);

            // 3. APLICAR NUEVO SALDO
            // Necesitamos recargar Cuenta/Categoria por si el usuario las cambió en el Edit
            var newAccount = await context.Account.FindAsync([dto.AccountID], cancellationToken);
            var newCategory = await context.Category.FindAsync([dto.CategoryID], cancellationToken);

            if (newAccount != null && newCategory != null)
            {
                if (newCategory.Type == "Gasto") newAccount.Balance -= dto.Amount;
                else if (newCategory.Type == "Ingreso") newAccount.Balance += dto.Amount;
            }

            await context.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}