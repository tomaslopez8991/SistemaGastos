using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.Transactions.Queries;

public record GetStatisticsQuery(int UserID) : IRequest<StatisticsDto>;