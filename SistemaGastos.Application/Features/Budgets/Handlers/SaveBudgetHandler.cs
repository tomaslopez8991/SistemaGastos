using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.Budgets.Commands;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.Budgets.Handlers;

public class SaveBudgetHandler(IApplicationDbContext context)
    : IRequestHandler<SaveBudgetCommand, bool>
{
    public async Task<bool> Handle(SaveBudgetCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var exists = await context.Budget.AnyAsync(b =>
            b.UserID == request.UserId &&
            b.CategoryID == dto.CategoryID &&
            b.PeriodMonth == dto.PeriodMonth &&
            b.PeriodYear == dto.PeriodYear &&
            b.ID != dto.ID, cancellationToken);

        if (exists)
            throw new InvalidOperationException("Ya tienes un presupuesto definido para esta categoría en ese mes.");

        if (dto.ID > 0)
        {
            var existing = await context.Budget.FindAsync([dto.ID], cancellationToken);
            if (existing == null) return false;

            existing.CategoryID = dto.CategoryID;
            existing.AmountLimit = dto.AmountLimit;
            existing.PeriodMonth = dto.PeriodMonth;
            existing.PeriodYear = dto.PeriodYear;

            context.Budget.Update(existing);
        }
        else
        {
            var budget = new Budget
            {
                UserID = request.UserId,
                CategoryID = dto.CategoryID,
                AmountLimit = dto.AmountLimit,
                PeriodMonth = dto.PeriodMonth,
                PeriodYear = dto.PeriodYear
            };
            context.Budget.Add(budget);
        }

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
