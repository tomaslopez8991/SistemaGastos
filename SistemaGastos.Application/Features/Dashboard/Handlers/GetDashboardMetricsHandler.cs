using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.Dashboard.Queries;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.Dashboard.Handlers;

public class GetDashboardMetricsHandler(IApplicationDbContext context, IDolarService dolarService)
    : IRequestHandler<GetDashboardMetricsQuery, DashboardVM>
{
    public async Task<DashboardVM> Handle(GetDashboardMetricsQuery request, CancellationToken cancellationToken)
    {
        var userId = await context.Login
            .AsNoTracking()
            .Where(u => u.Username == request.Username)
            .Select(u => u.ID)
            .FirstOrDefaultAsync(cancellationToken);

        if (userId == 0) return new DashboardVM();

        var hoy = DateTime.Now;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);

        var dolarTask = dolarService.GetDolarBolsaAsync();

        var saldo = await context.Account
            .AsNoTracking()
            .Where(a => a.UserID == userId && !a.Name.Contains("crédito"))
            .SumAsync(a => a.Balance, cancellationToken);

        var gastos = await context.Transaction
            .AsNoTracking()
            .Where(t => t.Account.Login.ID == userId
                        && t.Date >= inicioMes
                        && t.Category.Type == "Gasto")
            .SumAsync(t => t.Amount, cancellationToken);

        var ingresos = await context.Transaction
            .AsNoTracking()
            .Where(t => t.Account.Login.ID == userId
                        && t.Date >= inicioMes
                        && t.Category.Type == "Ingreso")
            .SumAsync(t => t.Amount, cancellationToken);

        var deudaArs = await context.CreditCardTransaction
            .AsNoTracking()
            .Where(t => t.Account.Login.ID == userId && t.Account.Currency == "ARS")
            .SumAsync(t => t.Amount, cancellationToken);

        var deudaUsd = await context.CreditCardTransaction
            .AsNoTracking()
            .Where(t => t.Account.Login.ID == userId && (t.Account.Currency == "USD" || t.Account.Currency == "USDT"))
            .SumAsync(t => t.Amount, cancellationToken);

        var tareas = await context.TodoTask
            .AsNoTracking()
            .CountAsync(t => t.Login.ID == userId && !t.IsCompleted, cancellationToken);

        var mesActualStr = hoy.ToString("yyyy-MM");
        var proximosGastos = await context.FixedExpense
            .AsNoTracking()
            .Where(fe => fe.UserID == userId
                         && fe.Active
                         && fe.PaymentDay >= hoy.Day
                         && (fe.PausedMonths == null || !fe.PausedMonths.Contains(mesActualStr)))
            .OrderBy(fe => fe.PaymentDay)
            .Take(5)
            .Select(fe => new ProximoGastoFijoDto(fe.Name, fe.Amount, fe.Currency, fe.PaymentDay, fe.LogoUrl))
            .ToListAsync(cancellationToken);

        decimal cotizacionDolar = await dolarTask;
        decimal deudaTotal = deudaArs + (deudaUsd * cotizacionDolar);

        return new DashboardVM
        {
            SaldoTotal = saldo,
            GastosMes = gastos,
            IngresosMes = ingresos,
            DeudaTarjetas = deudaTotal,
            TareasPendientes = tareas,
            ProximosGastosFijos = proximosGastos
        };
    }
}
