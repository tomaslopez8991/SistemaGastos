using MediatR;

namespace SistemaGastos.Application.Features.FixedExpense.Commands;

/// <summary>
/// Genera registros de Gasto Fijo para cada TC cuyo ClosingDay ya pasó en el mes indicado
/// y no tiene un registro generado para ese mes todavía.
/// </summary>
public record SyncCreditCardFixedExpensesCommand(int UserID, int Year, int Month) : IRequest<int>;
