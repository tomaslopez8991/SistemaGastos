using FluentValidation;
using SistemaGastos.Application.Features.Accounts.Commands;

namespace SistemaGastos.Application.Features.Accounts.Validators;

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.Dto.Name)
            .NotEmpty().WithMessage("El nombre de la cuenta es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder los 100 caracteres.");

        RuleFor(x => x.Dto.Currency)
            .NotEmpty().WithMessage("La moneda es obligatoria.")
            .Length(3).WithMessage("La moneda debe ser un código de 3 letras (ej: ARS, USD).");

        RuleFor(x => x.Dto.Balance)
            .NotNull().WithMessage("El saldo es obligatorio.");

        RuleFor(x => x.Dto.MinimumPaymentPercentage)
            .InclusiveBetween(0, 100).WithMessage("El porcentaje de pago mínimo debe estar entre 0 y 100.")
            .When(x => x.Dto.MinimumPaymentPercentage.HasValue);

        RuleFor(x => x.Dto.MinimumPaymentManualOverride)
            .GreaterThanOrEqualTo(0).WithMessage("El pago mínimo manual no puede ser negativo.")
            .When(x => x.Dto.MinimumPaymentManualOverride.HasValue);
    }
}