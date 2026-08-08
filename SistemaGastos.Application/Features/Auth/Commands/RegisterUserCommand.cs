using MediatR;
using System.ComponentModel.DataAnnotations;

namespace SistemaGastos.Application.Features.Auth.Commands;

public record RegisterUserCommand : IRequest<bool>
{
    public string Username { get; init; } = string.Empty;
    [EmailAddress] public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string OriginUrl { get; init; } = string.Empty;
}
