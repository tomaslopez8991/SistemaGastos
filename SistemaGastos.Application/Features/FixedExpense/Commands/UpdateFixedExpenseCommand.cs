using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.FixedExpense.Commands;

public record UpdateFixedExpenseCommand : IRequest<bool>
{
    public int ID { get; init; }
    public int UserID { get; init; }
    public string Name { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "ARS";
    public int AccountID { get; init; }
    public int CategoryID { get; init; }
    public int PaymentDay { get; init; }
    public string? LogoUrl { get; init; }
    public int? PersonID { get; init; }
    public decimal? PersonPercentage { get; init; }
    public DateTime? StartDate { get; init; }
    public int? DistributionEndDay { get; init; }
    public string? ExcludedDays { get; init; }
}