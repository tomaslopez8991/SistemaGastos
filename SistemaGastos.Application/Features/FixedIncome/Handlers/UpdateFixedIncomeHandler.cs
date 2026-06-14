using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.FixedIncome.Commands;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.FixedIncome.Handlers;

public class UpdateFixedIncomeHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateFixedIncomeCommand, bool>
{
    public async Task<bool> Handle(UpdateFixedIncomeCommand request, CancellationToken cancellationToken)
    {
        var income = await context.FixedIncome
            .FirstOrDefaultAsync(f => f.ID == request.ID && f.UserID == request.UserID, cancellationToken);

        if (income is null) return false;

        income.Name = request.Name;
        income.Amount = request.Amount;
        income.Currency = request.Currency;
        income.AccountID = request.AccountID;
        income.CategoryID = request.CategoryID;
        income.ReceiptDay = request.ReceiptDay;
        income.LogoUrl = request.LogoUrl;
        income.StartDate = request.StartDate;
        income.DistributionEndDay = request.DistributionEndDay;
        income.ExcludedDays = request.ExcludedDays;

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
