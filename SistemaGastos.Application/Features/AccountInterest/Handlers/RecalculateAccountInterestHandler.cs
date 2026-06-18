using MediatR;
using SistemaGastos.Application.Features.AccountInterest.Commands;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.AccountInterest.Handlers;

public class RecalculateAccountInterestHandler(IAccountInterestService accountInterestService)
    : IRequestHandler<RecalculateAccountInterestCommand, Unit>
{
    public async Task<Unit> Handle(RecalculateAccountInterestCommand request, CancellationToken cancellationToken)
    {
        await accountInterestService.RecalculateAsync(request.UserID, cancellationToken);
        return Unit.Value;
    }
}
