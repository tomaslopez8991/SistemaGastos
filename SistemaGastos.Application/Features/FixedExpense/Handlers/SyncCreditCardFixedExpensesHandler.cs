using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.FixedExpense.Commands;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Enums;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.FixedExpense.Handlers;

public class SyncCreditCardFixedExpensesHandler(IApplicationDbContext context)
    : IRequestHandler<SyncCreditCardFixedExpensesCommand, int>
{
    public async Task<int> Handle(SyncCreditCardFixedExpensesCommand request, CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var targetMonthKey = $"{request.Year}-{request.Month:D2}";

        // Solo sincronizar si el mes pedido es el mes actual (no tiene sentido generar para futuros)
        if (request.Year != today.Year || request.Month != today.Month)
            return 0;

        // Cargar cuentas TC del usuario
        var ccAccounts = await context.Account
            .Where(a => a.UserID == request.UserID && a.Type == AccountType.TarjetaCredito)
            .ToListAsync(cancellationToken);

        if (ccAccounts.Count == 0) return 0;

        // IDs de TC que ya tienen registro generado para este mes
        var existingCCIds = (await context.FixedExpense
            .Where(f => f.UserID == request.UserID
                     && f.PaymentYearMonth == targetMonthKey
                     && f.CreditCardAccountID != null)
            .Select(f => f.CreditCardAccountID!.Value)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        // Cuenta bancaria default para debitar el pago (primera no-TC)
        var defaultAccountID = await context.Account
            .Where(a => a.UserID == request.UserID && a.Type != AccountType.TarjetaCredito)
            .OrderBy(a => a.ID)
            .Select(a => a.ID)
            .FirstOrDefaultAsync(cancellationToken);

        if (defaultAccountID == 0) return 0;

        // Categoría default: primera categoría de tipo Gasto
        var defaultCategoryID = await context.Category
            .Where(c => c.Type != "Ingreso")
            .OrderBy(c => c.ID)
            .Select(c => c.ID)
            .FirstOrDefaultAsync(cancellationToken);

        if (defaultCategoryID == 0) return 0;

        int created = 0;

        foreach (var cc in ccAccounts)
        {
            // Solo si el cierre ya ocurrió este mes (hoy > ClosingDay)
            if (!cc.ClosingDay.HasValue || today.Day <= cc.ClosingDay.Value)
                continue;

            // Ya existe registro para este ciclo
            if (existingCCIds.Contains(cc.ID))
                continue;

            // Balance negativo = deuda; si no hay deuda, no generar registro
            var deuda = Math.Abs(cc.Balance);
            if (deuda <= 0) continue;

            var dueDay = cc.DueDay ?? cc.ClosingDay.Value + 10;

            var newExpense = new Domain.Models.FixedExpense
            {
                UserID = request.UserID,
                AccountID = defaultAccountID,
                CategoryID = defaultCategoryID,
                CreditCardAccountID = cc.ID,
                PaymentYearMonth = targetMonthKey,
                Name = $"Total TC - {cc.Name}",
                Amount = deuda,
                Currency = cc.Currency,
                PaymentDay = Math.Min(dueDay, 28),
                Active = true,
                StartDate = new DateTime(request.Year, request.Month, 1)
            };

            await context.FixedExpense.AddAsync(newExpense, cancellationToken);
            created++;
        }

        if (created > 0)
            await context.SaveChangesAsync(cancellationToken);

        return created;
    }
}
