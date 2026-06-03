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
            .Where(f => f.UserID == request.UserID)
            .OrderBy(f => f.ReceiptDay)
            .ToListAsync(cancellationToken);

        // Ingresos ya cobrados este mes/año (vía Transaction.FixedIncomeID)
        var receivedIds = await context.Transaction
            .AsNoTracking()
            .Where(t => t.FixedIncomeID != null
                     && t.Date.Year == request.Year
                     && t.Date.Month == request.Month)
            .Select(t => t.FixedIncomeID!.Value)
            .ToListAsync(cancellationToken);

        var receivedSet = receivedIds.ToHashSet();

        var monthName = new DateTimeFormatInfo { MonthNames = CultureInfo.GetCultureInfo("es-AR").DateTimeFormat.MonthNames }
            .GetMonthName(request.Month);
        if (!string.IsNullOrEmpty(monthName))
            monthName = char.ToUpper(monthName[0]) + monthName[1..];

        return incomes.Select(f =>
        {
            // Monto en ARS para mostrar
            decimal amountArs = f.Currency == "USD" ? f.Amount * dolarRate : f.Amount;

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
                AccountName = f.Account?.Name ?? "Sin cuenta",
                LogoUrl = f.LogoUrl,
                Active = f.Active,
                StartDate = f.StartDate,
                LastGeneratedDate = f.LastGeneratedDate,
                AlreadyReceivedThisMonth = receivedSet.Contains(f.ID),
                ReceivedMonthName = receivedSet.Contains(f.ID) ? monthName : null
            };
        }).ToList();
    }
}
