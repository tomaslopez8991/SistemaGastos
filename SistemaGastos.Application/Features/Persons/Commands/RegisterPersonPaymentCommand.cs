using MediatR;

namespace SistemaGastos.Application.Features.Persons.Commands;

public record RegisterPersonPaymentCommand(
    int PersonID,
    int UserID,
    int AccountID,
    decimal Amount
) : IRequest<bool>;
