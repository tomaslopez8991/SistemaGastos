using MediatR;

namespace SistemaGastos.Application.Features.Users.Commands;

// Para actualizar perfil (Email, etc)
public record UpdateUserProfileCommand(int Id, string Email) : IRequest<bool>;

// Para cambiar password
public record ChangeUserPasswordCommand(int UserId, string NewPassword) : IRequest<bool>;

// Para activar/desactivar (Fusionamos SetActive/SetInactive en uno solo)
public record ToggleUserStatusCommand(int UserId, bool IsActive) : IRequest<bool>;

// Para borrar usuario
public record DeleteUserCommand(int UserId) : IRequest<bool>;