using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.Transactions.Queries;

public record GetTransactionByIdQuery(int ID, int UserID) : IRequest<TransactionDTO>;
