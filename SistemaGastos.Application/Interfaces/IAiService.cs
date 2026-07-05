namespace SistemaGastos.Application.Interfaces;

public interface IAiService
{
    Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default);
}
