using MediatR;

namespace SistemaGastos.Application.Features.TmpTransactions.Commands;

public record ConfirmTmpTransactionCommand(long ID, int UserID) : IRequest<bool>;
