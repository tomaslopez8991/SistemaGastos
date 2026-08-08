using MediatR;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.Users.Queries;

public record GetAllUsersQuery(string CurrentUsername) : IRequest<List<AdminUserDto>>;
public record GetUserProfileQuery(string Username) : IRequest<Login?>;
