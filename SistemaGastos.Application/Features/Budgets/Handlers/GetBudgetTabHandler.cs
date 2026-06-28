using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.Budgets.Queries;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Application.ViewModels;

namespace SistemaGastos.Application.Features.Budgets.Handlers;

public class GetBudgetTabHandler(IApplicationDbContext context)
    : IRequestHandler<GetBudgetTabQuery, List<BudgetStatusViewModel>>
{
    public async Task<List<BudgetStatusViewModel>> Handle(GetBudgetTabQuery request, CancellationToken cancellationToken)
    {
        // EF Core DbContext no es thread-safe: queries secuenciales
        var budgets = await context.Budget
            .Include(b => b.Category)
            .Where(b => b.UserID == request.UserId && b.PeriodMonth == request.Month && b.PeriodYear == request.Year)
            .ToListAsync(cancellationToken);

        var cashExpenses = await context.Transaction
            .AsNoTracking()
            .Where(t => t.Account.UserID == request.UserId && t.Date.Month == request.Month && t.Date.Year == request.Year)
            .Select(t => new { t.CategoryID, t.Amount })
            .ToListAsync(cancellationToken);

        var cardExpenses = await context.CreditCardTransaction
            .AsNoTracking()
            .Include(t => t.Account)
            .Where(t => t.Account.UserID == request.UserId && t.TransactionDate.Month == request.Month && t.TransactionDate.Year == request.Year)
            .Select(t => new { t.CategoryID, t.Amount })
            .ToListAsync(cancellationToken);

        var now = DateTime.Now;
        int daysInMonth = DateTime.DaysInMonth(request.Year, request.Month);
        var model = new List<BudgetStatusViewModel>();

        foreach (var b in budgets)
        {
            var spentCash = cashExpenses.Where(t => t.CategoryID == b.CategoryID).Sum(t => t.Amount);
            var spentCard = cardExpenses.Where(t => t.CategoryID == b.CategoryID).Sum(t => t.Amount);
            decimal totalSpent = spentCash + spentCard;

            decimal projected = totalSpent;
            bool showAlert = false;
            decimal deviation = 0;

            if (request.Month == now.Month && request.Year == now.Year && now.Day > 1)
            {
                decimal dailyAverage = totalSpent / now.Day;
                projected = dailyAverage * daysInMonth;

                if (projected > b.AmountLimit && totalSpent <= b.AmountLimit)
                {
                    showAlert = true;
                    deviation = projected - b.AmountLimit;
                }
            }

            model.Add(new BudgetStatusViewModel
            {
                ID = b.ID,
                CategoryName = b.Category.Name,
                CategoryIcon = b.Category.Icon ?? "fa-solid fa-tag",
                CategoryColor = b.Category.Color ?? "#6c757d",
                Limit = b.AmountLimit,
                Spent = totalSpent,
                ProjectedAmount = projected,
                ShowPacingAlert = showAlert,
                PacingDeviation = deviation
            });
        }

        return model;
    }
}
