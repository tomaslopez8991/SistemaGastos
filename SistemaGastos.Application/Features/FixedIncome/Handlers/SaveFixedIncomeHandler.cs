using MediatR;
using SistemaGastos.Application.Features.FixedIncome.Commands;

namespace SistemaGastos.Application.Features.FixedIncome.Handlers;

public class SaveFixedIncomeHandler(IMediator mediator)
    : IRequestHandler<SaveFixedIncomeCommand, int>
{
    public async Task<int> Handle(SaveFixedIncomeCommand request, CancellationToken cancellationToken)
    {
        if (request.Dto.ID > 0)
        {
            var update = new UpdateFixedIncomeCommand
            {
                ID = request.Dto.ID,
                UserID = request.UserID,
                Name = request.Dto.Name,
                Amount = request.Dto.Amount,
                Currency = request.Dto.Currency,
                AccountID = request.Dto.AccountID,
                CategoryID = request.Dto.CategoryID,
                ReceiptDay = request.Dto.ReceiptDay,
                LogoUrl = request.Dto.LogoUrl,
                StartDate = request.Dto.StartDate,
                DistributionEndDay = request.Dto.DistributionEndDay,
                ExcludedDays = request.Dto.ExcludedDays
            };
            var ok = await mediator.Send(update, cancellationToken);
            return ok ? request.Dto.ID : 0;
        }
        else
        {
            var create = new CreateFixedIncomeCommand
            {
                UserID = request.UserID,
                Name = request.Dto.Name,
                Amount = request.Dto.Amount,
                Currency = request.Dto.Currency,
                AccountID = request.Dto.AccountID,
                CategoryID = request.Dto.CategoryID,
                ReceiptDay = request.Dto.ReceiptDay,
                LogoUrl = request.Dto.LogoUrl,
                StartDate = request.Dto.StartDate,
                DistributionEndDay = request.Dto.DistributionEndDay,
                ExcludedDays = request.Dto.ExcludedDays
            };
            return await mediator.Send(create, cancellationToken);
        }
    }
}
