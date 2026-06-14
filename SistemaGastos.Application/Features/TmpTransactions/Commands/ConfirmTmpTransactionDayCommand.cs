using MediatR;

namespace SistemaGastos.Application.Features.TmpTransactions.Commands;

public record ConfirmTmpTransactionDayCommand(long ID, int Day, int UserID) : IRequest<bool>;
