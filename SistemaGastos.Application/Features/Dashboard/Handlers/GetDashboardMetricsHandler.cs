using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.Dashboard.Queries;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Application.DTOs;
// Asegúrate de importar tu DolarService o su Interfaz

namespace SistemaGastos.Application.Features.Dashboard.Handlers;

public class GetDashboardMetricsHandler(IApplicationDbContext context, IDolarService dolarService, IAccountInterestService accountInterestService)
    : IRequestHandler<GetDashboardMetricsQuery, DashboardVM>
{
    public async Task<DashboardVM> Handle(GetDashboardMetricsQuery request, CancellationToken cancellationToken)
    {
        // 1. Obtener ID del usuario
        var userId = await context.Login
            .AsNoTracking()
            .Where(u => u.Username == request.Username)
            .Select(u => u.ID)
            .FirstOrDefaultAsync(cancellationToken);

        if (userId == 0) return new DashboardVM();

        // 2. Preparar fechas
        var hoy = DateTime.Now;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);

        // 3. EJECUCIÓN SECUENCIAL (Uno por uno para evitar error de hilos en DbContext)

        // A. Cotización Dólar (Este sí es externo, pero lo esperamos igual por orden)
        decimal cotizacionDolar = await dolarService.GetDolarBolsaAsync();

        // B. Saldo
        var saldo = await context.Account
            .AsNoTracking()
            .Where(a => a.UserID == userId && !a.Name.Contains("crédito"))
            .SumAsync(a => a.Balance, cancellationToken);

        // C. Gastos del Mes
        var gastos = await context.Transaction
            .AsNoTracking()
            .Where(t => t.Account.Login.ID == userId
                        && t.Date >= inicioMes
                        && t.Category.Type == "Gasto")
            .SumAsync(t => t.Amount, cancellationToken);

        // D. Deuda ARS
        var deudaArs = await context.CreditCardTransaction
            .AsNoTracking()
            .Where(t => t.Account.Login.ID == userId && t.Account.Currency == "ARS")
            .SumAsync(t => t.Amount, cancellationToken);

        // E. Deuda USD/USDT
        var deudaUsd = await context.CreditCardTransaction
            .AsNoTracking()
            .Where(t => t.Account.Login.ID == userId && (t.Account.Currency == "USD" || t.Account.Currency == "USDT"))
            .SumAsync(t => t.Amount, cancellationToken);

        // F. Tareas Pendientes
        var tareas = await context.TodoTask
            .AsNoTracking()
            .CountAsync(t => t.Login.ID == userId && !t.IsCompleted, cancellationToken);

        // F2. Intereses acumulados (cuentas con cálculo de intereses habilitado)
        var interesesAcumulados = await accountInterestService.GetTotalAccruedInterestAsync(userId, cancellationToken);

        // 4. Calcular Totales
        decimal deudaTotal = deudaArs + (deudaUsd * cotizacionDolar);

        // 5. Retornar DTO
        return new DashboardVM
        {
            SaldoTotal = saldo,
            GastosMes = gastos,
            DeudaTarjetas = deudaTotal,
            TareasPendientes = tareas,
            InteresesAcumulados = interesesAcumulados
        };
    }
}