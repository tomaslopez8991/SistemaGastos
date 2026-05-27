using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.Auth.Commands;
using SistemaGastos.Application.Helpers;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.Auth.Handlers;

public class RegisterUserHandler(IApplicationDbContext context)
    : IRequestHandler<RegisterUserCommand, bool>
{
    public async Task<bool> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var exists = await context.Login.AnyAsync(
            u => u.Username == request.Username || u.Email == request.Email,
            cancellationToken);

        if (exists)
            throw new InvalidOperationException("El usuario o correo electrónico ya está en uso.");

        var newUser = new Login
        {
            Username = request.Username,
            Email = request.Email,
            Password = SecurityHelper.HashPassword(request.Password),
            Role = "User",
            Active = true
        };

        context.Login.Add(newUser);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
