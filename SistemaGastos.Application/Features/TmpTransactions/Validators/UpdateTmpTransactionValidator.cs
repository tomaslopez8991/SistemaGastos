using FluentValidation;
using SistemaGastos.Application.Features.TmpTransactions.Commands;

namespace SistemaGastos.Application.Features.TmpTransactions.Validators;

public class UpdateTmpTransactionValidator : AbstractValidator<UpdateTmpTransactionCommand>
{
    public UpdateTmpTransactionValidator()
    {
        RuleFor(x => x.ID)
            .GreaterThan(0).WithMessage("ID inválido");

        RuleFor(x => x.UserID)
            .GreaterThan(0).WithMessage("Usuario inválido");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción es obligatoria")
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0");

        RuleFor(x => x.CategoryID)
            .GreaterThan(0).WithMessage("Debe seleccionar una categoría válida");

        RuleFor(x => x.DateTransaction)
            .NotNull().WithMessage("La fecha es obligatoria para editar");
    }
}
