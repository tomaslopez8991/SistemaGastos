using FluentValidation;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Validators;

public class CreateTransactionValidator : AbstractValidator<CreditCardTransactionDto>
{
    public CreateTransactionValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción es obligatoria.")
            .MaximumLength(100).WithMessage("La descripción no puede superar los 100 caracteres.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0.");

        RuleFor(x => x.CategoryID)
            .GreaterThan(0).WithMessage("Debes seleccionar una categoría válida.");

        RuleFor(x => x.AccountID)
            .GreaterThan(0).WithMessage("Debes seleccionar una cuenta válida.");

        // Ejemplo de regla compleja:
        // "Si es cuota fija, no puede tener más de 1 cuota" (depende de tu lógica)
        RuleFor(x => x.Installments)
            .GreaterThanOrEqualTo(1).WithMessage("Las cuotas deben ser al menos 1.")
            .Must((dto, cuotas) => !dto.Fixed || cuotas == 1)
            .WithMessage("Si el gasto es fijo, debe ser de 1 sola cuota.");
    }
}