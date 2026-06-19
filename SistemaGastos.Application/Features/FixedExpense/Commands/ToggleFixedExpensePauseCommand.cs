using MediatR;

namespace SistemaGastos.Application.Features.FixedExpense.Commands;

/// <summary>
/// Alterna la pausa de un gasto fijo para el mes especificado.
/// Si ya estaba pausado ese mes, lo reanuda; si no, lo pausa.
/// </summary>
public record ToggleFixedExpensePauseCommand(int ID, int UserID, int Year, int Month) : IRequest<bool>;
