using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.TmpTransactions.Queries;

public record GetDashboardStatsQuery(int UserID) : IRequest<DashboardStatsDto>;
