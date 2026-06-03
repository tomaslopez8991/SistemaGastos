using MediatR;

namespace SistemaGastos.Application.Features.FixedExpense.Commands;

/// <param name="ActivateFromDate">
/// Solo se usa al REANUDAR (Active = false → true).
/// Si se provee, el gasto fijo queda activo a partir de ese mes (actualiza StartDate).
/// Si es null al reanudar, se limpia StartDate (activo desde siempre).
/// Al pausar este campo se ignora.
/// </param>
public record ToggleFixedExpenseActiveCommand(int ID, int UserID, DateTime? ActivateFromDate = null) : IRequest<bool>;