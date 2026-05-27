using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.Dashboard.Queries;

public record GetDashboardMetricsQuery(string Username) : IRequest<DashboardVM>;