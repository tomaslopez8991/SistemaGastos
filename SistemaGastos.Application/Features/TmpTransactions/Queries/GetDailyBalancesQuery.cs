using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.TmpTransactions.Queries;

public record GetDailyBalancesQuery(int UserID, int Year, int Month) : IRequest<DailyCalendarDto>;
