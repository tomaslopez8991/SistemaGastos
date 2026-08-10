using FluentValidation;
using SistemaGastos.Application.Features.TmpTransactions.Commands;

namespace SistemaGastos.Application.Features.TmpTransactions.Validators;

public class CreateTmpTransactionValidator : AbstractValidator<CreateTmpTransactionCommand>
{
    public CreateTmpTransactionValidator()
    {
        RuleFor(x => x.UserID)
            .GreaterThan(0).WithMessage("Usuario inválido");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción es obligatoria")
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0");

        RuleFor(x => x.CategoryID)
            .GreaterThan(0).WithMessage("Debe seleccionar una categoría válida");

        RuleFor(x => x.AccountID)
            .NotNull().WithMessage("Debe seleccionar una cuenta")
            .GreaterThan(0).WithMessage("Debe seleccionar una cuenta válida");

        // Validación condicional para recurrentes
        When(x => x.EsRecurrente, () =>
        {
            RuleFor(x => x.MesesSeleccionados)
                .NotNull().WithMessage("Debe seleccionar al menos un mes")
                .Must(meses => meses != null && meses.Any())
                .WithMessage("Debe seleccionar al menos un mes para la recurrencia")
                .Must(meses => meses != null && meses.All(IsValidMonth))
                .WithMessage("Uno o más meses seleccionados no son válidos");
        });

        // Validación condicional para no recurrentes
        When(x => !x.EsRecurrente, () =>
        {
            RuleFor(x => x.DateTransaction)
                .NotNull().WithMessage("Debe indicar el mes de impacto");
        });
    }

    private static bool IsValidMonth(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;

        var parts = value.Split('-');
        return parts.Length == 2
            && int.TryParse(parts[0], out var year)
            && int.TryParse(parts[1], out var month)
            && year is >= 1 and <= 9999
            && month is >= 1 and <= 12;
    }
}
