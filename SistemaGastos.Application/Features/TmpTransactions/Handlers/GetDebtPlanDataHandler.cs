using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.TmpTransactions.Queries;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Enums;
using System.Globalization;

namespace SistemaGastos.Application.Features.TmpTransactions.Handlers;

public class GetDebtPlanDataHandler(IApplicationDbContext context, IMediator mediator, IDolarService dolarService)
    : IRequestHandler<GetDebtPlanDataQuery, DebtPlanDataDto>
{
    public async Task<DebtPlanDataDto> Handle(GetDebtPlanDataQuery request, CancellationToken cancellationToken)
    {
        var culture = new CultureInfo("es-AR");
        var now = DateTime.Now;
        var startDate = new DateTime(now.Year, now.Month, 1);
        decimal dolarRate = await dolarService.GetDolarBolsaAsync();

        // 1. Proyecciones base (reutiliza toda la lógica existente)
        var baseProjections = await mediator.Send(new GetProjectedBalancesQuery(request.UserID), cancellationToken);

        // 2. Saldo líquido actual (excluye tarjetas de crédito)
        var accounts = await context.Account
            .AsNoTracking()
            .Where(a => a.UserID == request.UserID)
            .ToListAsync(cancellationToken);

        decimal saldoARS = accounts
            .Where(a => a.Type != AccountType.TarjetaCredito && a.Currency == "ARS")
            .Sum(a => a.Balance);
        decimal saldoUSD = accounts
            .Where(a => a.Type != AccountType.TarjetaCredito && (a.Currency == "USD" || a.Currency == "USDT"))
            .Sum(a => a.Balance);
        decimal currentBalance = saldoARS + saldoUSD * dolarRate;

        // 3. Mapear proyecciones base al DTO del plan
        var months = baseProjections.Select(m => new DebtPlanMonthBaseDto
        {
            Key = m.Key,
            Label = m.Label,
            Month = m.Month,
            Year = m.Year,
            Income = m.Income,
            ManualExpenses = m.ManualExpenses,
            FixedExpensesTotal = m.FixedExpenses,
            PendingInstallments = m.PendingInstallments,
            PersonsReceivable = m.PersonsReceivable,
            BalanceBase = m.Balance
        }).ToList();

        // 4. Gastos fijos activos para el simulador
        var fixedExpenses = await context.FixedExpense
            .AsNoTracking()
            .Where(f => f.UserID == request.UserID && f.Active)
            .ToListAsync(cancellationToken);

        // Gastos ya pagados en el mes actual (para excluir el mes 0 si ya fue pagado)
        var paidThisMonth = await context.Transaction
            .AsNoTracking()
            .Where(t => t.Account.UserID == request.UserID
                     && t.FixedExpenseID != null
                     && t.Date.Year == now.Year
                     && t.Date.Month == now.Month)
            .Select(t => t.FixedExpenseID)
            .ToListAsync(cancellationToken);

        var fixedExpenseSims = new List<FixedExpenseSimDto>();
        foreach (var fe in fixedExpenses)
        {
            decimal amountARS = fe.Currency == "USD" ? fe.Amount * dolarRate : fe.Amount;
            var activeIndices = new List<int>();
            for (int i = 0; i < 12; i++)
            {
                var m = startDate.AddMonths(i);
                int daysInMonth = DateTime.DaysInMonth(m.Year, m.Month);
                bool validDay = fe.PaymentDay > 0 && fe.PaymentDay <= daysInMonth;
                bool inStartDate = fe.StartDate == null ||
                    new DateTime(fe.StartDate.Value.Year, fe.StartDate.Value.Month, 1) <= m;
                bool alreadyPaid = i == 0 && paidThisMonth.Contains(fe.ID);
                if (validDay && inStartDate && !alreadyPaid)
                    activeIndices.Add(i);
            }
            fixedExpenseSims.Add(new FixedExpenseSimDto
            {
                ID = fe.ID,
                Name = fe.Name,
                MonthlyAmountARS = amountARS,
                MonthlyAmountARSFmt = amountARS.ToString("C0", culture),
                LogoUrl = fe.LogoUrl,
                ActiveMonthIndices = activeIndices
            });
        }

        // Ordenar por monto descendente para mostrar los más relevantes primero
        fixedExpenseSims = fixedExpenseSims.OrderByDescending(f => f.MonthlyAmountARS).ToList();

        // 5. Última configuración guardada por el usuario (si existe)
        var savedSettings = await context.DebtPlanSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserID == request.UserID, cancellationToken);

        var settingsDto = savedSettings == null
            ? new DebtPlanSettingsDto()
            : new DebtPlanSettingsDto
            {
                GoalType = savedSettings.GoalType,
                GoalValue = savedSettings.GoalValue,
                ExtraMonthlyIncome = savedSettings.ExtraMonthlyIncome,
                ScenariosMode = savedSettings.ScenariosMode,
                ScenarioMin = savedSettings.ScenarioMin,
                ScenarioNormal = savedSettings.ScenarioNormal,
                ScenarioMax = savedSettings.ScenarioMax,
                RemovedFixedExpenseIds = string.IsNullOrWhiteSpace(savedSettings.RemovedFixedExpenseIds)
                    ? new List<int>()
                    : savedSettings.RemovedFixedExpenseIds
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s, out var id) ? id : (int?)null)
                        .Where(id => id.HasValue)
                        .Select(id => id!.Value)
                        .ToList()
            };

        return new DebtPlanDataDto
        {
            CurrentBalance = currentBalance,
            CurrentBalanceFmt = currentBalance.ToString("C0", culture),
            DolarRate = dolarRate,
            Months = months,
            FixedExpenses = fixedExpenseSims,
            Settings = settingsDto
        };
    }
}
