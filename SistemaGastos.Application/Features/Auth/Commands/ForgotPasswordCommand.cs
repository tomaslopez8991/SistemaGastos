using MediatR;
using System.ComponentModel.DataAnnotations;

namespace SistemaGastos.Application.Features.Auth.Commands;

// Retorna bool (True = proceso completado o usuario no existe, para seguridad)
public record ForgotPasswordCommand(
    [Required] string Email,
    string OriginUrl
) : IRequest<bool>;