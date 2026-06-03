using MediatR;

namespace SistemaGastos.Application.Features.TmpTransactions.Commands;

public record UpdateTmpTransactionCommand : IRequest<bool>
{
    public long ID { get; init; }
    public int UserID { get; init; }
    public string Description { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "ARS";
    public int CategoryID { get; init; }
    public int? AccountID { get; init; }
    public DateTime? DateTransaction { get; init; }
}
