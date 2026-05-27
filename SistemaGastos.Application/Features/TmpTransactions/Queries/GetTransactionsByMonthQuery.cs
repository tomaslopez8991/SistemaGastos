using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.TmpTransactions.Queries;

public record GetTransactionsByMonthQuery(int UserID, int Year, int Month) : IRequest<List<TmpTransactionDto>>;
