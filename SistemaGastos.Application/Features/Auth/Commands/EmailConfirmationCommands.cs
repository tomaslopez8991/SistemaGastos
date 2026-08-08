using MediatR;

namespace SistemaGastos.Application.Features.Auth.Commands;

public record ConfirmEmailCommand(string Token) : IRequest<bool>;
public record ResendConfirmationEmailCommand(string Email, string OriginUrl) : IRequest<bool>;
