using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.TmpTransactions.Queries;

public class GetTmpTransactionListQuery : IRequest<TmpTransactionListDto>
{
    public int UserID { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}
