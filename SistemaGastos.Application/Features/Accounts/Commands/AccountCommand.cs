using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.Accounts.Commands;

public record CreateAccountCommand(CreateAccountDto Dto) : IRequest<int>;

public record UpdateAccountCommand(UpdateAccountDto Dto) : IRequest<bool>;

public record DeleteAccountCommand(int Id) : IRequest<bool>;

public record PayCreditCardCommand(
    int TcAccountId,
    int SourceAccountId,
    decimal Amount,
    DateTime PaymentDate,
    int UserId,
    int? FixedExpenseId = null) : IRequest<PaymentResultDto>;

public record TransferMoneyCommand(TransferDto Dto) : IRequest<bool>;
