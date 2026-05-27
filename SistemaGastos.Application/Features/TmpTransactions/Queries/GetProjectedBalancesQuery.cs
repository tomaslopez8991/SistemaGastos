using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.TmpTransactions.Queries;

public record GetProjectedBalancesQuery(int UserID) : IRequest<List<MonthlyBalanceDto>>;
