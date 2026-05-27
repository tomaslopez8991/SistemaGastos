using MediatR;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.Users.Queries;

// Para obtener la lista de usuarios (Admin)
public record GetAllUsersQuery(string CurrentUsername) : IRequest<List<Login>>;

// Para obtener el perfil del usuario actual
public record GetUserProfileQuery(string Username) : IRequest<Login?>;