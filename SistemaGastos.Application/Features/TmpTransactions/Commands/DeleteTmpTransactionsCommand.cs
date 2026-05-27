using MediatR;

namespace SistemaGastos.Application.Features.TmpTransactions.Commands;

public record DeleteTmpTransactionsCommand(List<long> IDs, int UserID) : IRequest<int>;
