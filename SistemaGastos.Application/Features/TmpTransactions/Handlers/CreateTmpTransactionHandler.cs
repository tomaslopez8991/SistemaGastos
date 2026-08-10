using MediatR;
using SistemaGastos.Application.Features.TmpTransactions.Commands;
using SistemaGastos.Application.Interfaces;
using TmpTransactionEntity = SistemaGastos.Domain.Models.TmpTransaction;

namespace SistemaGastos.Application.Features.TmpTransactions.Handlers;

public class CreateTmpTransactionHandler(IApplicationDbContext context)
    : IRequestHandler<CreateTmpTransactionCommand, int>
{
    public async Task<int> Handle(CreateTmpTransactionCommand request, CancellationToken cancellationToken)
    {
        var toInsert = new List<TmpTransactionEntity>();

        if (request.EsRecurrente)
        {
            // Crear múltiples transacciones para los meses seleccionados
            var mesesDistinct = (request.MesesSeleccionados ?? [])
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToList();

            // Clientes anteriores podían omitir la fecha al seleccionar solo meses.
            var occurrenceDay = request.DateTransaction?.Day ?? DateTime.Today.Day;

            foreach (var mes in mesesDistinct)
            {
                var parts = mes.Split('-');
                if (parts.Length != 2) continue;
                if (!int.TryParse(parts[0], out int y) || !int.TryParse(parts[1], out int m)) continue;

                var day = Math.Min(occurrenceDay, DateTime.DaysInMonth(y, m));

                toInsert.Add(new TmpTransactionEntity
                {
                    Description = request.Description,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    CategoryID = request.CategoryID,
                    AccountID = request.AccountID!.Value,
                    UserID = request.UserID,
                    DateTransaction = new DateTime(y, m, day),
                    EsRecurrente = false,
                    DistributionEndDay = request.DistributionEndDay,
                    ExcludedDays = request.ExcludedDays
                });
            }
        }
        else
        {
            // Crear transacción única
            toInsert.Add(new TmpTransactionEntity
            {
                Description = request.Description,
                Amount = request.Amount,
                Currency = request.Currency,
                CategoryID = request.CategoryID,
                AccountID = request.AccountID!.Value,
                UserID = request.UserID,
                DateTransaction = request.DateTransaction!.Value,
                EsRecurrente = false,
                DistributionEndDay = request.DistributionEndDay,
                ExcludedDays = request.ExcludedDays
            });
        }

        if (toInsert.Count > 0)
        {
            await context.TmpTransaction.AddRangeAsync(toInsert, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        return toInsert.Count;
    }
}
