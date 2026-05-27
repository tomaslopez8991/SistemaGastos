using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.Transactions.Queries;

public record GetTransactionListQuery(
    int UserID,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int? AccountID = null,
    int? CategoryID = null
) : IRequest<TransactionListDto>;