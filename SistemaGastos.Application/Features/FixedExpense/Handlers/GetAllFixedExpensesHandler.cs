using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.FixedExpense.Queries;
using SistemaGastos.Application.Interfaces;
using System.Globalization;

namespace SistemaGastos.Application.Features.FixedExpense.Handlers;

public class GetAllFixedExpensesHandler(IApplicationDbContext context)
    : IRequestHandler<GetAllFixedExpensesQuery, List<FixedExpenseDto>>
{
    public async Task<List<FixedExpenseDto>> Handle(GetAllFixedExpensesQuery request, CancellationToken cancellationToken)
    {
        var culture = new CultureInfo("es-AR");

        var fixedExpenses = await context.FixedExpense
            .AsNoTracking()
            .Include(f => f.Category)
            .Include(f => f.Account)
            .Where(f => f.UserID == request.UserID)
            .OrderBy(f => f.PaymentDay) // ✅ Usar PaymentDay
            .ToListAsync(cancellationToken);

        return fixedExpenses.Select(f => new FixedExpenseDto
        {
            ID = f.ID,
            Name = f.Name ?? string.Empty, // ✅ Por si acaso
            Amount = f.Amount,
            AmountFormatted = f.Amount.ToString("C", culture),
            PaymentDay = f.PaymentDay,
            CategoryID = f.CategoryID,
            CategoryName = f.Category?.Name ?? "Sin categoría",
            AccountID = f.AccountID,
            AccountName = f.Account?.Name ?? "Sin cuenta",
            LogoUrl = f.LogoUrl,
            Active = f.Active,
            LastGeneratedDate = f.LastGeneratedDate
        }).ToList();
    }
}