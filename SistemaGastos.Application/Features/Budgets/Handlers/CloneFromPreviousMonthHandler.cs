using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.Budgets.Commands;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.Budgets.Handlers;

public class CloneFromPreviousMonthHandler(IApplicationDbContext context)
    : IRequestHandler<CloneFromPreviousMonthCommand, bool>
{
    public async Task<bool> Handle(CloneFromPreviousMonthCommand request, CancellationToken cancellationToken)
    {
        var prevDate = new DateTime(request.TargetYear, request.TargetMonth, 1).AddMonths(-1);

        var oldBudgets = await context.Budget
            .AsNoTracking()
            .Where(b => b.UserID == request.UserId && b.PeriodMonth == prevDate.Month && b.PeriodYear == prevDate.Year)
            .ToListAsync(cancellationToken);

        if (!oldBudgets.Any()) return false;

        var newBudgets = oldBudgets.Select(b => new Budget
        {
            UserID = b.UserID,
            CategoryID = b.CategoryID,
            AmountLimit = b.AmountLimit,
            PeriodMonth = request.TargetMonth,
            PeriodYear = request.TargetYear
        });

        context.Budget.AddRange(newBudgets);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
