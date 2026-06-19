using MediatR;

namespace SistemaGastos.Application.Features.FixedIncome.Commands;

/// <summary>
/// Alterna la pausa de un ingreso fijo para el mes especificado.
/// Si ya estaba pausado ese mes, lo reanuda; si no, lo pausa.
/// </summary>
public record ToggleFixedIncomePauseCommand(int ID, int UserID, int Year, int Month) : IRequest<bool>;
