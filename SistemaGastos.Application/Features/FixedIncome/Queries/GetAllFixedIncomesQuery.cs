using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.FixedIncome.Queries;

public record GetAllFixedIncomesQuery(int UserID, int Year, int Month) : IRequest<List<FixedIncomeDto>>;
