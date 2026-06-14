using MediatR;

namespace SistemaGastos.Application.Features.TmpTransactions.Commands;

public record CreateTmpTransactionCommand : IRequest<int>
{
    public int UserID { get; init; }
    public string Description { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "ARS";
    public int CategoryID { get; init; }
    public int? AccountID { get; init; }
    public DateTime? DateTransaction { get; init; }
    public bool EsRecurrente { get; init; }
    public List<string> MesesSeleccionados { get; init; }
    public int? DistributionEndDay { get; init; }
    public string? ExcludedDays { get; init; }
}
