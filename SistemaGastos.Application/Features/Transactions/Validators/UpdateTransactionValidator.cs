using FluentValidation;
using SistemaGastos.Application.Features.Transactions.Commands;

namespace SistemaGastos.Application.Features.Transactions.Validators;

public class UpdateTransactionValidator : AbstractValidator<UpdateTransactionCommand>
{
    public UpdateTransactionValidator()
    {
        RuleFor(x => x.ID)
            .GreaterThan(0).WithMessage("ID de transacción inválido");

        RuleFor(x => x.UserID)
            .GreaterThan(0).WithMessage("Usuario inválido");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("La fecha es obligatoria")
            .LessThanOrEqualTo(DateTime.Now.AddDays(1)).WithMessage("La fecha no puede ser futura");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0");


        RuleFor(x => x.AccountID)
            .GreaterThan(0).WithMessage("Debe seleccionar una cuenta válida");

        RuleFor(x => x.CategoryID)
            .GreaterThan(0).WithMessage("Debe seleccionar una categoría válida");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres");
    }
}
