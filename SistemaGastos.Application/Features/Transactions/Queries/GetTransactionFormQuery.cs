using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.Transactions.Queries;

public record GetTransactionFormQuery(int UserID, int? ID = null) : IRequest<TransactionFormDto>;
