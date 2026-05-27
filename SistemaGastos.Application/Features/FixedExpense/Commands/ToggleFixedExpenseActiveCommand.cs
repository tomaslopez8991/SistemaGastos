using MediatR;

namespace SistemaGastos.Application.Features.FixedExpense.Commands;

public record ToggleFixedExpenseActiveCommand(int ID, int UserID) : IRequest<bool>;