using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.TmpTransactions.Queries;

public record GetDebtPlanDataQuery(int UserID) : IRequest<DebtPlanDataDto>;
