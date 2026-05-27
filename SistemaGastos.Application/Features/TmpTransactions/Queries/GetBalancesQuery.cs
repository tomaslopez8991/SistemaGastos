using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.TmpTransactions.Queries;

public class GetBalancesQuery : IRequest<List<MonthBalanceDto>>
{
    public int UserID { get; set; }
}
