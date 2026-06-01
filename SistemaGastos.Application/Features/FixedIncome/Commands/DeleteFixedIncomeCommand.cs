using MediatR;

namespace SistemaGastos.Application.Features.FixedIncome.Commands;

public record DeleteFixedIncomeCommand(int ID, int UserID) : IRequest<bool>;
