using FluentValidation;
using SistemaGastos.Application.Features.Transactions.Commands;

namespace SistemaGastos.Application.Features.Transactions.Validators;

public class DeleteTransactionValidator : AbstractValidator<DeleteTransactionCommand>
{
    public DeleteTransactionValidator()
    {
        RuleFor(x => x.ID)
            .GreaterThan(0).WithMessage("ID de transacción inválido");

        RuleFor(x => x.UserID)
            .GreaterThan(0).WithMessage("Usuario inválido");
    }
}
