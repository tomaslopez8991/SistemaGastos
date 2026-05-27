using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.Transactions.Commands;

public record CreateTransactionCommand : IRequest<int>
{
    public int UserID { get; init; }
    public DateTime Date { get; init; }
    public decimal Amount { get; init; }
    public int AccountID { get; init; }
    public int CategoryID { get; init; }
    public string Description { get; init; }
    public int? FixedExpenseID { get; init; }
    public List<TransactionSplitDto> Splits { get; set; } = new List<TransactionSplitDto>();
}
