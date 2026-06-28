namespace SistemaGastos.Application.Interfaces;

public interface ICurrentUserService
{
    int? UserId { get; }
    string? Username { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
}