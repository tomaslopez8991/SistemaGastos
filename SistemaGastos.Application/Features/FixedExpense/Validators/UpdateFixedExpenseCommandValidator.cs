using FluentValidation;

namespace SistemaGastos.Application.Features.FixedExpense.Commands;

public class UpdateFixedExpenseCommandValidator : AbstractValidator<UpdateFixedExpenseCommand>
{
    public UpdateFixedExpenseCommandValidator()
    {
        RuleFor(x => x.ID)
            .GreaterThan(0).WithMessage("ID inválido");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0");

        RuleFor(x => x.AccountID)
            .GreaterThan(0).WithMessage("Debe seleccionar una cuenta válida");
    }
}
