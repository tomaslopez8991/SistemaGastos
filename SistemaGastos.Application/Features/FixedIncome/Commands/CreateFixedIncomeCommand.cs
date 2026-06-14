using MediatR;

namespace SistemaGastos.Application.Features.FixedIncome.Commands;

public record CreateFixedIncomeCommand : IRequest<int>
{
    public int UserID { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "ARS";
    public int AccountID { get; init; }
    public int CategoryID { get; init; }
    public int ReceiptDay { get; init; }
    public string? LogoUrl { get; init; }
    public DateTime? StartDate { get; init; }
    public int? DistributionEndDay { get; init; }
    public string? ExcludedDays { get; init; }
}
