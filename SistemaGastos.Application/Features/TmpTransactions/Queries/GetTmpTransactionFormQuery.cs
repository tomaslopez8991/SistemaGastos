using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.TmpTransactions.Queries;

public record GetTmpTransactionFormQuery(int UserID, long? ID = null) : IRequest<TmpTransactionFormDto>;
