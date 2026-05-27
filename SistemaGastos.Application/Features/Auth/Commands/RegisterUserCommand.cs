using MediatR;
using System.ComponentModel.DataAnnotations;

namespace SistemaGastos.Application.Features.Auth.Commands;

public record RegisterUserCommand : IRequest<bool>
{
    public string Username { get; init; }
    [EmailAddress] public string Email { get; init; }
    public string Password { get; init; }
}