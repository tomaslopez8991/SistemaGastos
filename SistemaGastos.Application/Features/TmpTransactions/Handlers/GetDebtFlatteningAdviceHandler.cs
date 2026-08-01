using MediatR;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.TmpTransactions.Queries;
using SistemaGastos.Application.Interfaces;
using System.Globalization;
using System.Text;

namespace SistemaGastos.Application.Features.TmpTransactions.Handlers;

public class GetDebtFlatteningAdviceHandler(IMediator mediator, IAiService aiService)
    : IRequestHandler<GetDebtFlatteningAdviceQuery, DebtAdviceDto>
{
    public async Task<DebtAdviceDto> Handle(GetDebtFlatteningAdviceQuery request, CancellationToken cancellationToken)
    {
        var snapshot = await mediator.Send(new GetDebtSnapshotQuery(request.UserID), cancellationToken);

        var prompt = BuildPrompt(snapshot, request.Question);
        var advice = await aiService.CompleteAsync(prompt, cancellationToken);

        return new DebtAdviceDto
        {
            Snapshot = snapshot,
            Advice = advice
        };
    }

    private static string BuildPrompt(DebtSnapshotDto snapshot, string? question)
    {
        var culture = new CultureInfo("es-AR");
        var sb = new StringBuilder();

        sb.AppendLine("Sos un asesor financiero personal. Con estos datos del próximo mes de un usuario:");
        sb.AppendLine();
        sb.AppendLine($"Ingreso proyectado: {snapshot.ProjectedIncomeNextMonth.ToString("C", culture)}");
        sb.AppendLine($"Excedente disponible estimado: {snapshot.ProjectedSurplusNextMonth.ToString("C", culture)}");
        sb.AppendLine($"Interés por descubierto acumulado: {snapshot.OverdraftInterestAccrued.ToString("C", culture)}");

        sb.AppendLine();
        sb.AppendLine("Deudas activas por tarjeta (ordenadas por saldo, de mayor a menor):");
        if (snapshot.CardDebts.Count == 0)
        {
            sb.AppendLine("- Sin deuda activa en tarjetas.");
        }
        else
        {
            foreach (var card in snapshot.CardDebts)
            {
                var minimumPaymentText = card.MinimumPaymentDue.HasValue
                    ? $"pago mínimo {card.MinimumPaymentDue.Value.ToString("C", culture)}"
                    : "sin pago mínimo configurado";

                sb.AppendLine($"- {card.AccountName}: saldo {card.CurrentBalance.ToString("C", culture)} ({card.Currency}), " +
                    $"próximo vencimiento {card.NextDueDate:dd/MM/yyyy}, {minimumPaymentText}, " +
                    $"{card.InstallmentsRemainingCount} compra(s) en cuotas por {card.RemainingInstallmentsTotal.ToString("C", culture)} restante(s).");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Gastos fijos pendientes este mes:");
        if (snapshot.PendingFixedExpenses.Count == 0)
        {
            sb.AppendLine("- Ninguno pendiente.");
        }
        else
        {
            foreach (var fe in snapshot.PendingFixedExpenses)
            {
                sb.AppendLine($"- {fe.Name}: {fe.Amount.ToString("C", culture)} ({fe.Currency}), vence el día {fe.PaymentDay}.");
            }
        }

        sb.AppendLine();

        if (string.IsNullOrWhiteSpace(question))
        {
            sb.AppendLine("""
                Con esta información, sugerí qué pagos adelantar o qué deuda atacar primero
                para reducir la carga del mes siguiente. Priorizá por impacto real (vencimientos
                próximos, evitar o reducir el interés por descubierto, tarjetas con mayor saldo).
                Distinguí claramente el pago mínimo (lo obligatorio para no caer en mora o
                recargos) del excedente disponible para aplanar la deuda más rápido pagando
                por encima del mínimo. Dame como máximo 3 recomendaciones concretas y
                accionables, en español, con montos aproximados. No repitas los datos crudos,
                andá directo a la recomendación.
                """);
        }
        else
        {
            sb.AppendLine("Con esta información, respondé en español y de forma concreta la siguiente pregunta del usuario:");
            sb.AppendLine(question);
        }

        return sb.ToString();
    }
}
