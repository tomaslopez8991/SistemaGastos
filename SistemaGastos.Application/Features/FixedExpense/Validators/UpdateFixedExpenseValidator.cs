using FluentValidation;
using SistemaGastos.Application.Features.FixedExpense.Commands;

namespace SistemaGastos.Application.Features.FixedExpense.Validators;

public class UpdateFixedExpenseValidator : AbstractValidator<UpdateFixedExpenseCommand>
{
    public UpdateFixedExpenseValidator()
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

        RuleFor(x => x.CategoryID)
            .GreaterThan(0).WithMessage("Debe seleccionar una categoría válida");

        RuleFor(x => x.PaymentDay)
            .InclusiveBetween(1, 31).WithMessage("El día de pago debe estar entre 1 y 31");

        RuleFor(x => x.UserID)
            .GreaterThan(0).WithMessage("Usuario inválido");
    }
}
