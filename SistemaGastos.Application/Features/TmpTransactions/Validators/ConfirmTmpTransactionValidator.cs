using FluentValidation;
using SistemaGastos.Application.Features.TmpTransactions.Commands;

namespace SistemaGastos.Application.Features.TmpTransactions.Validators;

public class ConfirmTmpTransactionValidator : AbstractValidator<ConfirmTmpTransactionCommand>
{
    public ConfirmTmpTransactionValidator()
    {
        RuleFor(x => x.ID)
            .GreaterThan(0).WithMessage("ID de transacción inválido");

        RuleFor(x => x.UserID)
            .GreaterThan(0).WithMessage("Usuario inválido");
    }
}
