// GetCreditCardTransactionsQuery.cs
using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.Transactions.Queries;

// Recibe el DTO y devuelve bool
public record UpdateCreditCardTransactionCommand(UpdateCreditCardTransactionDto Dto) : IRequest<bool>;


// Recibe lista de IDs
public record DeleteTransactionsCommand(List<long> Ids) : IRequest<bool>;

public record GetCreditCardTransactionsQuery(
    string? Keyword,
    int Limit,
    int Offset,
    int? CategoryID = null,
    string? CategoryName = null,
    int? PersonID = null,
    int? Installments = null,
    bool? Fixed = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null
) : IRequest<CreditCardTransactionSearchResultDto>;

public record CreditCardTransactionSearchResultDto(
    List<CreditCardTransactionDto> Results,
    int Total,
    CreditCardTotalsDto Totals);
