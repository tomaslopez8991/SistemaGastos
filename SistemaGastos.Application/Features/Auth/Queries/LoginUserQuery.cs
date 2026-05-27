using MediatR;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.Auth.Queries;

public record LoginUserQuery(string Username, string Password) : IRequest<Login?>;