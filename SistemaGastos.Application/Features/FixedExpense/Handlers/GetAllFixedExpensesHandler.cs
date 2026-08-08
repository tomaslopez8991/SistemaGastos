using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.FixedExpense.Queries;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Application.Helpers;
using SistemaGastos.Domain.Enums;
using System.Globalization;

namespace SistemaGastos.Application.Features.FixedExpense.Handlers;

public class GetAllFixedExpensesHandler(
    IApplicationDbContext context,
    IDolarService dolarService,
    IAccountInterestService accountInterestService)
    : IRequestHandler<GetAllFixedExpensesQuery, List<FixedExpenseDto>>
{
    public async Task<List<FixedExpenseDto>> Handle(GetAllFixedExpensesQuery request, CancellationToken cancellationToken)
    {
        var culture = new CultureInfo("es-AR");
        await accountInterestService.RunAccrualAsync(cancellationToken);

        // El call HTTP al dólar corre en paralelo con las queries de DB.
        // EF Core DbContext no es thread-safe: las queries de DB van secuenciales.
        var dolarTask = dolarService.GetDolarBolsaAsync();

        var currentMonthKey = $"{request.Year}-{request.Month:D2}";

        var fixedExpenses = await context.FixedExpense
            .AsNoTracking()
            .Include(f => f.Category)
            .Include(f => f.Account)
            .Where(f => f.UserID == request.UserID
                     && (f.PaymentYearMonth == null || f.PaymentYearMonth == currentMonthKey))
            .OrderBy(f => f.PaymentDay)
            .ToListAsync(cancellationToken);

        fixedExpenses = fixedExpenses
            .Where(f => f.Active || !InterestExpenseHelper.IsAutomaticOverdraftCharge(f))
            .ToList();

        var paidViaTransaction = await context.Transaction
            .AsNoTracking()
            .Where(t => t.FixedExpenseID != null
                     && t.Date.Year == request.Year
                     && t.Date.Month == request.Month)
            .Select(t => new { ExpenseID = t.FixedExpenseID!.Value, t.Amount, Currency = "ARS" })
            .ToListAsync(cancellationToken);

        var paidViaCreditCard = await context.CreditCardTransaction
            .AsNoTracking()
            .Where(t => t.FixedExpenseID != null
                     && t.TransactionDate.Year == request.Year
                     && t.TransactionDate.Month == request.Month)
            .Select(t => new { ExpenseID = t.FixedExpenseID!.Value, t.Amount, Currency = t.Account.Currency })
            .ToListAsync(cancellationToken);

        decimal dolarRate = await dolarTask;

        var ccAccounts = await context.Account
            .AsNoTracking()
            .Where(a => a.UserID == request.UserID && a.Type == AccountType.TarjetaCredito)
            .ToListAsync(cancellationToken);
        var ccById = ccAccounts.ToDictionary(a => a.ID);

        var paidIds = paidViaTransaction.Select(t => t.ExpenseID)
            .Union(paidViaCreditCard.Select(t => t.ExpenseID))
            .ToHashSet();

        // Monto real pagado por ID (prioriza Transaction normal sobre CreditCard)
        var paidAmounts = paidViaTransaction
            .GroupBy(t => t.ExpenseID)
            .ToDictionary(g => g.Key, g => (Amount: g.Sum(t => t.Amount), Currency: "ARS"));
        foreach (var cc in paidViaCreditCard.Where(cc => !paidAmounts.ContainsKey(cc.ExpenseID)))
            paidAmounts[cc.ExpenseID] = (cc.Amount, cc.Currency);

        var monthName = new DateTimeFormatInfo { MonthNames = CultureInfo.GetCultureInfo("es-AR").DateTimeFormat.MonthNames }
            .GetMonthName(request.Month);

        // Capitalizar primera letra
        if (!string.IsNullOrEmpty(monthName))
            monthName = char.ToUpper(monthName[0]) + monthName[1..];

        return fixedExpenses.Select(f =>
        {
            decimal amountArs = f.Currency == "USD" ? f.Amount * dolarRate : f.Amount;
            string amountFmt = f.Currency == "USD"
                ? $"USD {f.Amount:N2} (≈ {amountArs.ToString("C", culture)})"
                : f.Amount.ToString("C", culture);

            bool isPaused = !string.IsNullOrEmpty(f.PausedMonths) &&
                f.PausedMonths.Split(',').Select(s => s.Trim()).Contains(currentMonthKey);

            bool isPaid = f.CreditCardAccountID.HasValue
                ? f.Amount <= 0
                : paidIds.Contains(f.ID);
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

            decimal? tcMinimumAmount = null;
            decimal? tcTotalAmount = null;
            if (f.CreditCardAccountID.HasValue && ccById.ContainsKey(f.CreditCardAccountID.Value))
            {
                var ccAcc = ccById[f.CreditCardAccountID.Value];
                tcTotalAmount = ccAcc.Currency == "USD"
                    ? Math.Abs(ccAcc.Balance) * dolarRate
                    : Math.Abs(ccAcc.Balance);
                tcMinimumAmount = ccAcc.EffectiveMinimumPayment.HasValue
                    ? (ccAcc.Currency == "USD"
                        ? ccAcc.EffectiveMinimumPayment.Value * dolarRate
                        : ccAcc.EffectiveMinimumPayment.Value)
                    : null;
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
                PausedMonths = f.PausedMonths,
                CreditCardAccountID = f.CreditCardAccountID,
                PaymentYearMonth = f.PaymentYearMonth,
                TcMinimumAmount = tcMinimumAmount,
                TcTotalAmount = tcTotalAmount,
                IsSystemGenerated = InterestExpenseHelper.IsLiveAccrual(f, DateTime.Today)
            };
        }).ToList();
    }
}
