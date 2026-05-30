using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Notifications.Queries;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.Notifications.Handlers;

public class GetAllNotificationsHandler(IApplicationDbContext context)
    : IRequestHandler<GetAllNotificationsQuery, List<NotificationDto>>
{
    public async Task<List<NotificationDto>> Handle(GetAllNotificationsQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var notifications = new List<NotificationDto>();

        // ── 1. TAREAS vencidas o con recordatorio activo ──────────────────────
        var tasks = await context.TodoTask
            .AsNoTracking()
            .Where(t => t.UserID == request.UserId && !t.IsCompleted)
            .Where(t => t.DueDate <= tomorrow || (t.ReminderDate != null && t.ReminderDate <= today))
            .OrderBy(t => t.DueDate)
            .ToListAsync(cancellationToken);

        foreach (var t in tasks)
        {
            var isOverdue = t.DueDate < today;
            string subtitle;
            if (isOverdue)
                subtitle = $"Venció el {t.DueDate:dd/MM/yyyy}";
            else if (t.DueDate.Date == today)
                subtitle = "Vence hoy";
            else
                subtitle = "Vence mañana";

            notifications.Add(new NotificationDto(
                Type: "task",
                RefId: t.TaskId,
                Title: t.Title,
                Subtitle: subtitle,
                Icon: "fa-list-check",
                Severity: isOverdue ? "danger" : "warning",
                ActionUrl: "/Todo/Index"
            ));
        }

        // ── 2. GASTOS FIJOS activos no pagados este mes y próximos o vencidos ─
        var fixedExpenses = await context.FixedExpense
            .AsNoTracking()
            .Where(f => f.UserID == request.UserId && f.Active)
            .Where(f => f.LastGeneratedDate == null
                || f.LastGeneratedDate.Value.Year < today.Year
                || (f.LastGeneratedDate.Value.Year == today.Year
                    && f.LastGeneratedDate.Value.Month < today.Month))
            .ToListAsync(cancellationToken);

        foreach (var f in fixedExpenses)
        {
            int daysLeft = f.PaymentDay - today.Day;

            if (daysLeft < 0)
            {
                // Pasó el día de pago este mes sin procesar
                notifications.Add(new NotificationDto(
                    Type: "fixedexpense",
                    RefId: f.ID,
                    Title: f.Name,
                    Subtitle: $"Venció el día {f.PaymentDay} — ${f.Amount:N0}",
                    Icon: "fa-receipt",
                    Severity: "danger",
                    ActionUrl: "/TmpTransaction/Index#fijos"
                ));
            }
            else if (daysLeft <= 5)
            {
                var subtitle = daysLeft == 0
                    ? $"Vence hoy — ${f.Amount:N0}"
                    : $"Vence en {daysLeft} día{(daysLeft > 1 ? "s" : "")} — ${f.Amount:N0}";

                notifications.Add(new NotificationDto(
                    Type: "fixedexpense",
                    RefId: f.ID,
                    Title: f.Name,
                    Subtitle: subtitle,
                    Icon: "fa-receipt",
                    Severity: daysLeft <= 1 ? "warning" : "info",
                    ActionUrl: "/TmpTransaction/Index#fijos"
                ));
            }
        }

        // ── 3. PRESUPUESTOS del mes actual >= 80% gastado ─────────────────────
        var budgets = await context.Budget
            .AsNoTracking()
            .Include(b => b.Category)
            .Where(b => b.UserID == request.UserId
                && b.PeriodMonth == today.Month
                && b.PeriodYear == today.Year
                && b.AmountLimit > 0)
            .ToListAsync(cancellationToken);

        if (budgets.Count > 0)
        {
            var cashExpenses = await context.Transaction
                .AsNoTracking()
                .Where(t => t.Account.UserID == request.UserId
                    && t.Date.Month == today.Month
                    && t.Date.Year == today.Year)
                .Select(t => new { t.CategoryID, t.Amount })
                .ToListAsync(cancellationToken);

            var cardExpenses = await context.CreditCardTransaction
                .AsNoTracking()
                .Include(t => t.Account)
                .Where(t => t.Account.UserID == request.UserId
                    && t.TransactionDate.Month == today.Month
                    && t.TransactionDate.Year == today.Year)
                .Select(t => new { t.CategoryID, t.Amount })
                .ToListAsync(cancellationToken);

            foreach (var b in budgets)
            {
                var spent = cashExpenses.Where(t => t.CategoryID == b.CategoryID).Sum(t => t.Amount)
                          + cardExpenses.Where(t => t.CategoryID == b.CategoryID).Sum(t => t.Amount);

                var pct = (int)((spent / b.AmountLimit) * 100);
                if (pct < 80) continue;

                var isExceeded = pct >= 100;
                notifications.Add(new NotificationDto(
                    Type: "budget",
                    RefId: b.ID,
                    Title: b.Category?.Name ?? "Presupuesto",
                    Subtitle: isExceeded
                        ? $"¡Límite superado! {pct}% gastado"
                        : $"{pct}% del presupuesto utilizado",
                    Icon: "fa-chart-pie",
                    Severity: isExceeded ? "danger" : "warning",
                    ActionUrl: "/Budget/Index"
                ));
            }
        }

        // ── 4. CUENTAS con saldo negativo ─────────────────────────────────────
        var negativeAccounts = await context.Account
            .AsNoTracking()
            .Where(a => a.UserID == request.UserId && a.Balance < 0)
            .ToListAsync(cancellationToken);

        foreach (var a in negativeAccounts)
        {
            notifications.Add(new NotificationDto(
                Type: "balance",
                RefId: a.ID,
                Title: a.Name,
                Subtitle: $"Saldo negativo: ${a.Balance:N2}",
                Icon: "fa-building-columns",
                Severity: "danger",
                ActionUrl: "/Account/Index"
            ));
        }

        // Ordenar: danger primero, luego warning, luego info
        return notifications
            .OrderBy(n => n.Severity switch { "danger" => 0, "warning" => 1, _ => 2 })
            .ThenBy(n => n.Type)
            .ToList();
    }
}
