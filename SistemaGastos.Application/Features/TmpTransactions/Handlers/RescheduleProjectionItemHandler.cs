using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.TmpTransactions.Commands;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Enums;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.TmpTransactions.Handlers;

public class RescheduleProjectionItemHandler(IApplicationDbContext context)
    : IRequestHandler<RescheduleProjectionItemCommand, bool>
{
    private static readonly HashSet<string> SupportedTypes =
        ["Planificado", "GastoFijo", "IngresoFijo", "Personas", "TarjetaCredito"];

    public async Task<bool> Handle(RescheduleProjectionItemCommand request, CancellationToken cancellationToken)
    {
        if (request.UserID <= 0 || request.Year is < 1 or > 9999 || request.Month is < 1 or > 12)
            return false;
        if (!SupportedTypes.Contains(request.SourceType) || request.SourceID <= 0)
            return false;

        var daysInMonth = DateTime.DaysInMonth(request.Year, request.Month);
        if (request.OriginalDay < 1 || request.OriginalDay > daysInMonth
            || request.TargetDay < 1 || request.TargetDay > daysInMonth)
            return false;

        if (request.SourceType == "Planificado" && !request.IsDistributed)
        {
            var transaction = await context.TmpTransaction.FirstOrDefaultAsync(
                x => x.ID == request.SourceID && x.UserID == request.UserID && x.DateTransaction.HasValue,
                cancellationToken);
            if (transaction is null) return false;
            if (transaction.DateTransaction!.Value.Year != request.Year
                || transaction.DateTransaction.Value.Month != request.Month)
                return false;

            transaction.DateTransaction = new DateTime(request.Year, request.Month, request.TargetDay);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }

        if (!await SourceBelongsToUser(request, cancellationToken)) return false;

        var yearMonth = $"{request.Year}-{request.Month:D2}";
        var existing = await context.ProjectionScheduleOverride.FirstOrDefaultAsync(x =>
            x.UserID == request.UserID
            && x.SourceType == request.SourceType
            && x.SourceID == request.SourceID
            && x.YearMonth == yearMonth
            && x.OriginalDay == request.OriginalDay,
            cancellationToken);

        if (request.TargetDay == request.OriginalDay)
        {
            if (existing is not null)
            {
                context.ProjectionScheduleOverride.Remove(existing);
                await context.SaveChangesAsync(cancellationToken);
            }
            return true;
        }

        if (existing is null)
        {
            existing = new ProjectionScheduleOverride
            {
                UserID = request.UserID,
                SourceType = request.SourceType,
                SourceID = request.SourceID,
                YearMonth = yearMonth,
                OriginalDay = request.OriginalDay
            };
            context.ProjectionScheduleOverride.Add(existing);
        }

        existing.TargetDay = request.TargetDay;
        existing.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private Task<bool> SourceBelongsToUser(RescheduleProjectionItemCommand request, CancellationToken cancellationToken)
        => request.SourceType switch
        {
            "Planificado" => context.TmpTransaction.AnyAsync(x => x.ID == request.SourceID && x.UserID == request.UserID, cancellationToken),
            "GastoFijo" => context.FixedExpense.AnyAsync(x => x.ID == request.SourceID && x.UserID == request.UserID, cancellationToken),
            "IngresoFijo" => context.FixedIncome.AnyAsync(x => x.ID == request.SourceID && x.UserID == request.UserID, cancellationToken),
            "Personas" => context.Person.AnyAsync(x => x.ID == request.SourceID && x.UserID == request.UserID, cancellationToken),
            "TarjetaCredito" => context.Account.AnyAsync(x => x.ID == request.SourceID
                && x.UserID == request.UserID && x.Type == AccountType.TarjetaCredito, cancellationToken),
            _ => Task.FromResult(false)
        };
}
