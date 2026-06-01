using MediatR;

namespace SistemaGastos.Application.Features.FixedIncome.Commands;

public record ToggleFixedIncomeActiveCommand(int ID, int UserID) : IRequest<bool>;
