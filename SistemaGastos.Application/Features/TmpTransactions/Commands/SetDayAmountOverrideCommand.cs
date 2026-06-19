using MediatR;

namespace SistemaGastos.Application.Features.TmpTransactions.Commands;

/// <summary>
/// Establece o limpia el override de monto para un día específico de una transacción distribuida.
/// Amount = null limpia el override (vuelve al monto auto-calculado).
/// </summary>
public record SetDayAmountOverrideCommand(int TmpTransactionID, int UserID, int Day, decimal? Amount) : IRequest<bool>;
