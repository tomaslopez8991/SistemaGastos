using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.FixedExpense.Queries;
using SistemaGastos.Application.Interfaces;
using System.Globalization;

namespace SistemaGastos.Application.Features.FixedExpense.Handlers;

public class GetAllFixedExpensesHandler(IApplicationDbContext context, IDolarService dolarService)
    : IRequestHandler<GetAllFixedExpensesQuery, List<FixedExpenseDto>>
{
    public async Task<List<FixedExpenseDto>> Handle(GetAllFixedExpensesQuery request, CancellationToken cancellationToken)
    {
        var culture = new CultureInfo("es-AR");
        decimal dolarRate = await dolarService.GetDolarBolsaAsync();

        var fixedExpenses = await context.FixedExpense
            .AsNoTracking()
            .Include(f => f.Category)
            .Include(f => f.Account)
            .Where(f => f.UserID == request.UserID)
            .OrderBy(f => f.PaymentDay)
            .ToListAsync(cancellationToken);

        // Gastos fijos pagados este mes vía Transaction normal → guardamos el monto real abonado
        var paidViaTransaction = await context.Transaction
            .AsNoTracking()
            .Where(t => t.FixedExpenseID != null
                     && t.Date.Year == request.Year
                     && t.Date.Month == request.Month)
            .Select(t => new { ExpenseID = t.FixedExpenseID!.Value, t.Amount, Currency = "ARS" })
            .ToListAsync(cancellationToken);

        // Gastos fijos pagados este mes vía CreditCardTransaction
        var paidViaCreditCard = await context.CreditCardTransaction
            .AsNoTracking()
            .Where(t => t.FixedExpenseID != null
                     && t.TransactionDate.Year == request.Year
                     && t.TransactionDate.Month == request.Month)
            .Select(t => new { ExpenseID = t.FixedExpenseID!.Value, t.Amount, Currency = t.Account.Currency })
            .ToListAsync(cancellationToken);

        var paidIds = paidViaTransaction.Select(t => t.ExpenseID)
            .Union(paidViaCreditCard.Select(t => t.ExpenseID))
            .ToHashSet();

        // Monto real pagado por ID (prioriza Transaction normal sobre CreditCard)
        var paidAmounts = paidViaTransaction
            .ToDictionary(t => t.ExpenseID, t => (Amount: t.Amount, Currency: t.Currency));
        foreach (var cc in paidViaCreditCard.Where(cc => !paidAmounts.ContainsKey(cc.ExpenseID)))
            paidAmounts[cc.ExpenseID] = (cc.Amount, cc.Currency);

        var monthName = new DateTimeFormatInfo { MonthNames = CultureInfo.GetCultureInfo("es-AR").DateTimeFormat.MonthNames }
            .GetMonthName(request.Month);

        // Capitalizar primera letra
        if (!string.IsNullOrEmpty(monthName))
            monthName = char.ToUpper(monthName[0]) + monthName[1..];

        var currentMonthKey = $"{request.Year}-{request.Month:D2}";

        return fixedExpenses.Select(f =>
        {
            decimal amountArs = f.Currency == "USD" ? f.Amount * dolarRate : f.Amount;
            string amountFmt = f.Currency == "USD"
                ? $"USD {f.Amount:N2} (≈ {amountArs.ToString("C", culture)})"
                : f.Amount.ToString("C", culture);

            bool isPaused = !string.IsNullOrEmpty(f.PausedMonths) &&
                f.PausedMonths.Split(',').Select(s => s.Trim()).Contains(currentMonthKey);

            bool isPaid = paidIds.Contains(f.ID);
            decimal? paidAmount = null;
            string? paidAmountFmt = null;

            if (isPaid && paidAmounts.TryGetValue(f.ID, out var paid))
            {
                paidAmount = paid.Amount;
                decimal paidArs = paid.Currency == "USD" ? paid.Amount * dolarRate : paid.Amount;
                paidAmountFmt = paid.Currency == "USD"
                    ? $"USD {paid.Amount:N2} (≈ {paidArs.ToString("C", culture)})"
                    : paid.Amount.ToString("C", culture);
            }

            return new FixedExpenseDto
            {
                ID = f.ID,
                Name = f.Name ?? string.Empty,
                Amount = f.Amount,
                AmountFormatted = amountFmt,
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
                AlreadyPaidThisMonth = isPaid,
                PaidMonthName = isPaid ? monthName : null,
                PaidAmount = paidAmount,
                PaidAmountFormatted = paidAmountFmt,
                IsPausedThisMonth = isPaused,
                PausedMonths = f.PausedMonths
            };
        }).ToList();
    }
}
