using MediatR;

namespace SistemaGastos.Application.Features.Users.Commands;

public record UpdateUserProfileCommand(int Id, string Email) : IRequest<bool>;
public record ChangeUserPasswordCommand(int UserId, string NewPassword) : IRequest<bool>;
public record ToggleUserStatusCommand(int UserId, bool IsActive) : IRequest<bool>;
public record SetUserRoleCommand(int UserId, string Role) : IRequest<bool>;
public record SetUserDeletedCommand(int UserId, bool IsDeleted) : IRequest<bool>;
public record ResendUserConfirmationCommand(int UserId, string OriginUrl) : IRequest<bool>;
