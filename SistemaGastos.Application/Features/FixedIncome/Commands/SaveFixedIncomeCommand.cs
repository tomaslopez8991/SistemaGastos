using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.FixedIncome.Commands;

public record SaveFixedIncomeCommand(FixedIncomeDto Dto, int UserID) : IRequest<int>;
