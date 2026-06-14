using MediatR;
using SistemaGastos.Application.Features.FixedIncome.Commands;
using SistemaGastos.Application.Interfaces;
using FixedIncomeEntity = SistemaGastos.Domain.Models.FixedIncome;

namespace SistemaGastos.Application.Features.FixedIncome.Handlers;

public class CreateFixedIncomeHandler(IApplicationDbContext context)
    : IRequestHandler<CreateFixedIncomeCommand, int>
{
    public async Task<int> Handle(CreateFixedIncomeCommand request, CancellationToken cancellationToken)
    {
        var income = new FixedIncomeEntity
        {
            UserID = request.UserID,
            Name = request.Name,
            Amount = request.Amount,
            Currency = request.Currency,
            AccountID = request.AccountID,
            CategoryID = request.CategoryID,
            ReceiptDay = request.ReceiptDay,
            LogoUrl = request.LogoUrl,
            StartDate = request.StartDate,
            Active = true,
            DistributionEndDay = request.DistributionEndDay,
            ExcludedDays = request.ExcludedDays
        };

        await context.FixedIncome.AddAsync(income, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return income.ID;
    }
}
