using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Budgets.Queries;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.Budgets.Handlers;

public class GetBudgetFormHandler(IApplicationDbContext context)
    : IRequestHandler<GetBudgetFormQuery, BudgetFormDto>
{
    public async Task<BudgetFormDto> Handle(GetBudgetFormQuery request, CancellationToken cancellationToken)
    {
        var categories = await context.Category
            .AsNoTracking()
            .Where(c => c.Type == "Gasto" || c.Type == "Salidas")
            .OrderBy(c => c.Name)
            .Select(c => new BudgetCategoryLookupDto(c.ID, c.Name))
            .ToListAsync(cancellationToken);

        Domain.Models.Budget? budget = null;
        if (request.Id.HasValue)
            budget = await context.Budget.FindAsync([request.Id.Value], cancellationToken);

        int month = request.Month > 0 ? request.Month : DateTime.Now.Month;
        int year = request.Year > 0 ? request.Year : DateTime.Now.Year;

        return new BudgetFormDto(budget, categories, month, year);
    }
}
