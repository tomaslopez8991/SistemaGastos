using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.FixedIncome.Queries;

public record GetFixedIncomeFormQuery(int UserID, int? ID = null) : IRequest<FixedIncomeFormDto>;
