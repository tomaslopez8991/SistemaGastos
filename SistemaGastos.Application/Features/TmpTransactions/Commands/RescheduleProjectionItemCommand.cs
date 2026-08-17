using MediatR;

namespace SistemaGastos.Application.Features.TmpTransactions.Commands;

public record RescheduleProjectionItemCommand(
    int UserID,
    string SourceType,
    long SourceID,
    int Year,
    int Month,
    int OriginalDay,
    int TargetDay,
    bool IsDistributed) : IRequest<bool>;
