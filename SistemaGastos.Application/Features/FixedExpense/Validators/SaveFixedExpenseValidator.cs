using FluentValidation;
using SistemaGastos.Application.Features.FixedExpense.Commands;

namespace SistemaGastos.Application.Features.FixedExpense.Validators;

public class SaveFixedExpenseValidator : AbstractValidator<SaveFixedExpenseCommand>
{
    public SaveFixedExpenseValidator()
    {
        RuleFor(x => x.Dto)
            .NotNull().WithMessage("Los datos del gasto son obligatorios");

        RuleFor(x => x.UserID)
            .GreaterThan(0).WithMessage("Usuario inválido");

        RuleFor(x => x.Dto.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres")
            .When(x => x.Dto != null);

        RuleFor(x => x.Dto.Amount)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0")
            .When(x => x.Dto != null);

        RuleFor(x => x.Dto.AccountID)
            .GreaterThan(0).WithMessage("Debe seleccionar una cuenta válida")
            .When(x => x.Dto != null);

        RuleFor(x => x.Dto.CategoryID)
            .GreaterThan(0).WithMessage("Debe seleccionar una categoría válida")
            .When(x => x.Dto != null);

        RuleFor(x => x.Dto.PaymentDay)
            .InclusiveBetween(1, 31).WithMessage("El día de pago debe estar entre 1 y 31")
            .When(x => x.Dto != null);
    }
}
