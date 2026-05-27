using FluentValidation;
using SistemaGastos.Application.Features.TmpTransactions.Commands;

namespace SistemaGastos.Application.Features.TmpTransactions.Validators;

public class DeleteTmpTransactionsValidator : AbstractValidator<DeleteTmpTransactionsCommand>
{
    public DeleteTmpTransactionsValidator()
    {
        RuleFor(x => x.IDs)
            .NotNull().WithMessage("Debe seleccionar al menos una transacción")
            .Must(ids => ids != null && ids.Any())
            .WithMessage("Debe seleccionar al menos una transacción para eliminar");

        RuleFor(x => x.UserID)
            .GreaterThan(0).WithMessage("Usuario inválido");
    }
}
