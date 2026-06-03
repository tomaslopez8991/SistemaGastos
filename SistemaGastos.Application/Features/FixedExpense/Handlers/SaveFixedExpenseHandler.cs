using MediatR;
using SistemaGastos.Application.Features.FixedExpense.Commands;

namespace SistemaGastos.Application.Features.FixedExpense.Handlers;

public class SaveFixedExpenseHandler(IMediator mediator)
    : IRequestHandler<SaveFixedExpenseCommand, int>
{
    public async Task<int> Handle(SaveFixedExpenseCommand request, CancellationToken cancellationToken)
    {
        if (request.Dto.ID > 0)
        {
            // Actualizar
            var updateCommand = new UpdateFixedExpenseCommand
            {
                ID = request.Dto.ID,
                UserID = request.UserID,
                Name = request.Dto.Name,
                Amount = request.Dto.Amount,
                Currency = request.Dto.Currency,
                AccountID = request.Dto.AccountID,
                CategoryID = request.Dto.CategoryID,
                PaymentDay = request.Dto.PaymentDay,
                LogoUrl = request.Dto.LogoUrl,
                PersonID = request.Dto.PersonID,
                PersonPercentage = request.Dto.PersonPercentage
                StartDate = request.Dto.StartDate
            };

            var success = await mediator.Send(updateCommand, cancellationToken);
            return success ? request.Dto.ID : 0;
        }
        else
        {
            // Crear
            var createCommand = new CreateFixedExpenseCommand
            {
                UserID = request.UserID,
                Name = request.Dto.Name,
                Amount = request.Dto.Amount,
                Currency = request.Dto.Currency,
                AccountID = request.Dto.AccountID,
                CategoryID = request.Dto.CategoryID,
                PaymentDay = request.Dto.PaymentDay,
                LogoUrl = request.Dto.LogoUrl,
                PersonID = request.Dto.PersonID,
                PersonPercentage = request.Dto.PersonPercentage
                StartDate = request.Dto.StartDate
            };

            return await mediator.Send(createCommand, cancellationToken);
        }
    }
}