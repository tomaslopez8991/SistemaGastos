using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.Transactions.Commands;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.Transactions.Handlers;

// IRequestHandler<Comando, Respuesta>
public class CreateBulkCreditCardTransactionsHandler(IApplicationDbContext context, IMapper mapper)
    : IRequestHandler<CreateBulkCreditCardTransactionsCommand, bool>
{
    public async Task<bool> Handle(CreateBulkCreditCardTransactionsCommand request, CancellationToken cancellationToken)
    {
        // 1. Iniciamos la transacción de BD AQUÍ (en la lógica de negocio)
        using var dbTransaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var dtos = request.Transactions;

            // 2. Optimizaciones (Copiado de lo que hicimos antes)
            var accountIds = dtos.Select(d => d.AccountID).Distinct().ToList();
            var categoryIds = dtos.Select(d => d.CategoryID).Distinct().ToList();

            var accounts = await context.Account
                .Where(a => accountIds.Contains(a.ID))
                .ToListAsync(cancellationToken);

            var categories = await context.Category
                .Where(c => categoryIds.Contains(c.ID))
                .ToListAsync(cancellationToken);

            // 3. Procesamiento
            foreach (var dto in dtos)
            {
                var account = accounts.FirstOrDefault(a => a.ID == dto.AccountID);
                var category = categories.FirstOrDefault(c => c.ID == dto.CategoryID);

                if (account == null || category == null)
                    throw new Exception($"Datos inconsistentes para: {dto.Description}");

                // Mapeo Automático
                var entity = mapper.Map<CreditCardTransaction>(dto);

                // Lógica de Negocio (Saldos)
                if (category.Type == "Gasto") account.Balance -= dto.Amount;
                else if (category.Type == "Ingreso") account.Balance += dto.Amount;

                context.CreditCardTransaction.Add(entity);
            }

            await context.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);

            return true; // Éxito
        }
        catch (Exception)
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw; // Re-lanzamos la excepción para que el controlador o un middleware la capturen
        }
    }
}