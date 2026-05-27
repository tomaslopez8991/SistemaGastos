using MediatR;

namespace SistemaGastos.Application.Features.Transactions.Commands;

public record DeleteTransactionCommand(int ID, int UserID) : IRequest<bool>;
