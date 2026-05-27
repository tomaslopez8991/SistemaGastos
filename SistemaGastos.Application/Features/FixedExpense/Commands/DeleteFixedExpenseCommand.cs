using MediatR;

namespace SistemaGastos.Application.Features.FixedExpense.Commands;

public record DeleteFixedExpenseCommand(int ID, int UserID) : IRequest<bool>;
