using FluentValidation;
using MediatR;

namespace SistemaGastos.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // 1. Si no hay validadores para este comando, sigue adelante.
        if (!validators.Any())
        {
            return await next();
        }

        // 2. Ejecutar todas las validaciones
        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(r => r.Errors.Any())
            .SelectMany(r => r.Errors)
            .ToList();

        // 3. Si hay fallos, Lanzamos la excepción.
        if (failures.Any())
        {
            // FluentValidation tiene su propia excepción "ValidationException"
            throw new ValidationException(failures);
        }

        // 4. Si todo está bien, pasamos al siguiente paso (el Handler)
        return await next();
    }
}