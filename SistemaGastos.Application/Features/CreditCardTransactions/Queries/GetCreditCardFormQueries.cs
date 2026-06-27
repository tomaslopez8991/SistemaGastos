using MediatR;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.ViewModels;

namespace SistemaGastos.Application.Features.Transactions.Queries;

public record GetCreditCardIndexQuery(int UserId) : IRequest<CreditCardTransactionIndexVM>;
public record GetCreditCardTransactionFormQuery(int UserId, int? Id) : IRequest<CreditCardFormDto>;
public record GetMultipleCreditCardFormQuery(int UserId) : IRequest<CreditCardFormDto>;
public record GetCreditCardTotalsQuery(int UserId) : IRequest<CreditCardTotalsDto>;
