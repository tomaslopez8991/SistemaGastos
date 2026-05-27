using FluentValidation;
using SistemaGastos.Application.Features.FixedExpense.Commands;

namespace SistemaGastos.Application.Features.FixedExpense.Validators;

public class ProcessFixedExpensePaymentValidator : AbstractValidator<ProcessFixedExpensePaymentCommand>
{
    public ProcessFixedExpensePaymentValidator()
    {
        RuleFor(x => x.FixedExpenseID)
            .GreaterThan(0).WithMessage("ID de gasto fijo inválido");

        RuleFor(x => x.UserID)
            .GreaterThan(0).WithMessage("Usuario inválido");
    }
}
