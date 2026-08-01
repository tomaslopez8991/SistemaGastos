using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.FixedIncome.Queries;
using SistemaGastos.Application.Interfaces;
using System.Globalization;

namespace SistemaGastos.Application.Features.FixedIncome.Handlers;

public class GetAllFixedIncomesHandler(IApplicationDbContext context, IDolarService dolarService)
    : IRequestHandler<GetAllFixedIncomesQuery, List<FixedIncomeDto>>
{
    public async Task<List<FixedIncomeDto>> Handle(GetAllFixedIncomesQuery request, CancellationToken cancellationToken)
    {
        var culture = new CultureInfo("es-AR");
        decimal dolarRate = await dolarService.GetDolarBolsaAsync();

        var incomes = await context.FixedIncome
            .AsNoTracking()
            .Include(f => f.Category)
            .Include(f => f.Account)
            .Where(f => f.UserID == request.UserID
                     && (f.PersonID == null || f.CollectionYearMonth == $"{request.Year}-{request.Month:D2}"))
            .OrderBy(f => f.ReceiptDay)
            .ToListAsync(cancellationToken);

        // Ingresos ya cobrados este mes/año → guardamos el monto real cobrado
        var receivedTransactions = await context.Transaction
            .AsNoTracking()
            .Where(t => t.FixedIncomeID != null
                     && t.Date.Year == request.Year
                     && t.Date.Month == request.Month)
            .Select(t => new { IncomeID = t.FixedIncomeID!.Value, t.Amount, Currency = "ARS" })
            .ToListAsync(cancellationToken);

        var receivedSet = receivedTransactions.Select(t => t.IncomeID).ToHashSet();
        var receivedAmounts = receivedTransactions
            .ToDictionary(t => t.IncomeID, t => (Amount: t.Amount, Currency: t.Currency));

        var monthName = new DateTimeFormatInfo { MonthNames = CultureInfo.GetCultureInfo("es-AR").DateTimeFormat.MonthNames }
            .GetMonthName(request.Month);
        if (!string.IsNullOrEmpty(monthName))
            monthName = char.ToUpper(monthName[0]) + monthName[1..];

        var currentMonthKey = $"{request.Year}-{request.Month:D2}";

        return incomes.Select(f =>
        {
            decimal amountArs = f.Currency == "USD" ? f.Amount * dolarRate : f.Amount;

            bool isPaused = !string.IsNullOrEmpty(f.PausedMonths) &&
                f.PausedMonths.Split(',').Select(s => s.Trim()).Contains(currentMonthKey);

            bool isReceived = receivedSet.Contains(f.ID);
            decimal? receivedAmount = null;
            string? receivedAmountFmt = null;

            if (isReceived && receivedAmounts.TryGetValue(f.ID, out var received))
            {
                receivedAmount = received.Amount;
                decimal receivedArs = received.Currency == "USD" ? received.Amount * dolarRate : received.Amount;
                receivedAmountFmt = received.Currency == "USD"
                    ? $"USD {received.Amount:N2} ≈ {receivedArs.ToString("C", culture)}"
                    : receivedArs.ToString("C", culture);
            }

            return new FixedIncomeDto
            {
                ID = f.ID,
                Name = f.Name,
                Amount = f.Amount,
                AmountFormatted = f.Currency == "USD"
                    ? $"USD {f.Amount:N2} ≈ {amountArs.ToString("C", culture)}"
                    : amountArs.ToString("C", culture),
                Currency = f.Currency,
                ReceiptDay = f.ReceiptDay,
                CategoryID = f.CategoryID,
                CategoryName = f.Category?.Name ?? "Sin categoría",
                AccountID = f.AccountID,
                AccountName = f.PersonID.HasValue ? "A elegir al cobrar" : f.Account?.Name ?? "Sin cuenta",
                LogoUrl = f.LogoUrl,
                Active = f.Active,
                StartDate = f.StartDate,
                LastGeneratedDate = f.LastGeneratedDate,
                AlreadyReceivedThisMonth = isReceived,
                ReceivedMonthName = isReceived ? monthName : null,
                ReceivedAmount = receivedAmount,
                ReceivedAmountFormatted = receivedAmountFmt,
                IsPausedThisMonth = isPaused,
                PausedMonths = f.PausedMonths,
                PersonID = f.PersonID,
                CollectionYearMonth = f.CollectionYearMonth
            };
        }).ToList();
    }
}
