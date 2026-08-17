using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.TmpTransactions.Commands;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Enums;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.TmpTransactions.Handlers;

public class SetCreditCardProjectionScenarioHandler(IApplicationDbContext context)
    : IRequestHandler<SetCreditCardProjectionScenarioCommand, bool>
{
    public async Task<bool> Handle(SetCreditCardProjectionScenarioCommand request, CancellationToken cancellationToken)
    {
        if (request.Month is < 1 or > 12) return false;
        if (!Enum.IsDefined(request.Mode)) return false;
        if (!Enum.IsDefined(request.DistributionStrategy)) return false;
        if (request.Mode == TcProjectionMode.Personalizado && (!request.CustomAmount.HasValue || request.CustomAmount <= 0))
            return false;

        var accountExists = await context.Account.AnyAsync(a => a.ID == request.AccountID
            && a.UserID == request.UserID && a.Type == AccountType.TarjetaCredito, cancellationToken);
        if (!accountExists) return false;

        var key = $"{request.Year}-{request.Month:D2}";
        var scenario = await context.CreditCardProjectionScenario
            .FirstOrDefaultAsync(x => x.UserID == request.UserID
                && x.AccountID == request.AccountID && x.YearMonth == key, cancellationToken);

        if (scenario is null)
        {
            scenario = new CreditCardProjectionScenario
            {
                AccountID = request.AccountID,
                UserID = request.UserID,
                YearMonth = key
            };
            context.CreditCardProjectionScenario.Add(scenario);
        }

        scenario.Mode = request.Mode;
        scenario.DistributionStrategy = request.DistributionStrategy;
        scenario.CustomAmount = request.Mode == TcProjectionMode.Personalizado ? request.CustomAmount : null;
        scenario.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
