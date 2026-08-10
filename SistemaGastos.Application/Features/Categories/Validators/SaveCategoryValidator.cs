using FluentValidation;
using SistemaGastos.Application.Features.Categories.Commands;

namespace SistemaGastos.Application.Features.Categories.Validators;

public class SaveCategoryValidator : AbstractValidator<SaveCategoryCommand>
{
    private static readonly string[] ValidTypes = ["Gasto", "Ingreso"];

    public SaveCategoryValidator()
    {
        RuleFor(x => x.Dto.ID).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Dto.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(60).WithMessage("El nombre no puede superar los 60 caracteres.");
        RuleFor(x => x.Dto.Type)
            .Must(type => ValidTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Seleccioná un tipo válido.");
        RuleFor(x => x.Dto.Description)
            .MaximumLength(180).WithMessage("La descripción no puede superar los 180 caracteres.");
        RuleFor(x => x.Dto.Icon)
            .NotEmpty().WithMessage("Seleccioná un ícono.")
            .MaximumLength(80)
            .Matches(@"^fa-(solid|regular|brands)(\s+fa-[a-z0-9-]+)+$")
            .WithMessage("Ingresá una clase válida de Font Awesome.");
        RuleFor(x => x.Dto.Color)
            .Matches("^#[0-9A-Fa-f]{6}$")
            .WithMessage("Seleccioná un color válido.");
    }
}
