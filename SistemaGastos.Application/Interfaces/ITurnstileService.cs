namespace SistemaGastos.Application.Interfaces;

public interface ITurnstileService
{
    Task<bool> ValidateAsync(string? token, string? remoteIp, CancellationToken cancellationToken = default);
}
