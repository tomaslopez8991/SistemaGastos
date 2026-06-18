using MediatR;

namespace SistemaGastos.Application.Features.AccountInterest.Commands;

public record RecalculateAccountInterestCommand(int UserID) : IRequest<Unit>;
