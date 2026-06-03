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
            .OrderBy(f => f.PaymentDay)
            .ToListAsync(cancellationToken);

        // IDs de gastos fijos pagados en el mes/año solicitado (vía Transaction normal)
        var paidViaTransaction = await context.Transaction
            .AsNoTracking()
            .Where(t => t.FixedExpenseID != null
                     && t.Date.Year == request.Year
                     && t.Date.Month == request.Month)
            .Select(t => t.FixedExpenseID!.Value)
            .ToListAsync(cancellationToken);

        // IDs de gastos fijos pagados en el mes/año solicitado (vía CreditCardTransaction)
        var paidViaCreditCard = await context.CreditCardTransaction
            .AsNoTracking()
            .Where(t => t.FixedExpenseID != null
                     && t.TransactionDate.Year == request.Year
                     && t.TransactionDate.Month == request.Month)
            .Select(t => t.FixedExpenseID!.Value)
            .ToListAsync(cancellationToken);

        var paidIds = paidViaTransaction.Union(paidViaCreditCard).ToHashSet();

        var monthName = new DateTimeFormatInfo { MonthNames = CultureInfo.GetCultureInfo("es-AR").DateTimeFormat.MonthNames }
            .GetMonthName(request.Month);

        // Capitalizar primera letra
        if (!string.IsNullOrEmpty(monthName))
            monthName = char.ToUpper(monthName[0]) + monthName[1..];

        return fixedExpenses.Select(f => new FixedExpenseDto
        {
            ID = f.ID,
            Name = f.Name ?? string.Empty,
            Amount = f.Amount,
            AmountFormatted = f.Amount.ToString("C", culture),
            Currency = f.Currency,
            PaymentDay = f.PaymentDay,
            CategoryID = f.CategoryID,
            CategoryName = f.Category?.Name ?? "Sin categoría",
            AccountID = f.AccountID,
            AccountName = f.Account?.Name ?? "Sin cuenta",
            LogoUrl = f.LogoUrl,
            Active = f.Active,
            StartDate = f.StartDate,
            LastGeneratedDate = f.LastGeneratedDate,
            AlreadyPaidThisMonth = paidIds.Contains(f.ID),
            PaidMonthName = paidIds.Contains(f.ID) ? monthName : null
        }).ToList();
    }
}
