using System.Diagnostics;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Behaviors;

public class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
    IApplicationDbContext context)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
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

            try
            {
                var log = new PerformanceLog
                {
                    HandlerName = requestName,
                    ElapsedMs = elapsed,
                    RequestData = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = false }),
                    CreatedAt = DateTime.UtcNow
                };
                context.PerformanceLog.Add(log);
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[PERF LOG] Error al guardar PerformanceLog para {RequestName}", requestName);
            }
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
