using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace SistemaGastos.Application.Behaviors;

public class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    // Umbral en ms a partir del cual se considera lento y se loguea como Warning
    private const int SlowRequestThresholdMs = 500;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        var elapsed = sw.ElapsedMilliseconds;
        var requestName = typeof(TRequest).Name;

        if (elapsed >= SlowRequestThresholdMs)
        {
            logger.LogWarning(
                "[SLOW HANDLER] {RequestName} tardó {ElapsedMs}ms (umbral: {ThresholdMs}ms). Request: {@Request}",
                requestName, elapsed, SlowRequestThresholdMs, request);
        }
        else
        {
            logger.LogDebug(
                "[HANDLER] {RequestName} completado en {ElapsedMs}ms",
                requestName, elapsed);
        }

        return response;
    }
}
