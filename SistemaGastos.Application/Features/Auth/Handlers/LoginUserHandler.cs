using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.Auth.Queries;
using SistemaGastos.Application.Helpers;
using SistemaGastos.Application.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace SistemaGastos.Application.Features.Auth.Handlers;

public class LoginUserHandler(IApplicationDbContext context)
    : IRequestHandler<LoginUserQuery, Domain.Models.Login?>
{
    public async Task<Domain.Models.Login?> Handle(LoginUserQuery request, CancellationToken cancellationToken)
    {
        string passEncriptado = SecurityHelper.HashPassword(request.Password);

        var usuario = await context.Login
            .FirstOrDefaultAsync(u =>
                u.Username == request.Username &&
                u.Password == passEncriptado &&
                !u.IsDeleted,
                cancellationToken);

        return usuario;
    }
}
