using MediatR;
using SistemaGastos.Application.Features.FixedExpense.Commands;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.FixedExpense.Handlers;

public class CreateFixedExpenseHandler(IApplicationDbContext context)
    : IRequestHandler<CreateFixedExpenseCommand, int>
{
    public async Task<int> Handle(CreateFixedExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = new SistemaGastos.Domain.Models.FixedExpense
        {
            UserID = request.UserID,
            Name = request.Name,
            Amount = request.Amount,
            Currency = request.Currency,
            AccountID = request.AccountID,
            CategoryID = request.CategoryID,
            PaymentDay = request.PaymentDay,
            LogoUrl = request.LogoUrl,
            Active = true,
            PersonID = request.PersonID,
            PersonPercentage = request.PersonID.HasValue ? (request.PersonPercentage ?? 100m) : null,
            StartDate = request.StartDate,
            DistributionEndDay = request.DistributionEndDay,
            ExcludedDays = request.ExcludedDays
        };

        await context.FixedExpense.AddAsync(expense, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return expense.ID;
    }
}
