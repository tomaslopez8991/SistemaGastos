using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Budgets.Queries;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.Budgets.Handlers;

public class GetBudgetDetailsHandler(IApplicationDbContext context)
    : IRequestHandler<GetBudgetDetailsQuery, BudgetDetailsResult?>
{
    public async Task<BudgetDetailsResult?> Handle(GetBudgetDetailsQuery request, CancellationToken cancellationToken)
    {
        var budget = await context.Budget
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.ID == request.BudgetId, cancellationToken);

        if (budget == null) return null;

        var cashItems = await context.Transaction
            .AsNoTracking()
            .Include(t => t.Account)
            .Where(t => t.Account.UserID == budget.UserID &&
                        t.CategoryID == budget.CategoryID &&
                        t.Date.Month == budget.PeriodMonth &&
                        t.Date.Year == budget.PeriodYear)
            .Select(t => new BudgetDetailItemDto(t.Date, t.Description, t.Account.Name, t.Amount, false))
            .ToListAsync(cancellationToken);

        var cardItems = await context.CreditCardTransaction
            .AsNoTracking()
            .Include(t => t.Account)
            .Where(t => t.Account.UserID == budget.UserID &&
                        t.CategoryID == budget.CategoryID &&
                        t.TransactionDate.Month == budget.PeriodMonth &&
                        t.TransactionDate.Year == budget.PeriodYear)
            .Select(t => new BudgetDetailItemDto(t.TransactionDate, t.Description, t.Account.Name, t.Amount, true))
            .ToListAsync(cancellationToken);

        var allItems = cashItems.Concat(cardItems).OrderByDescending(x => x.Date).ToList();

        return new BudgetDetailsResult(
            allItems,
            budget.Category.Name,
            $"{budget.PeriodMonth}/{budget.PeriodYear}");
    }
}
