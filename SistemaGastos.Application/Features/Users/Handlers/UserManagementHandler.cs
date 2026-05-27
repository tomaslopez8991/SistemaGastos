using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.Users.Commands;
using SistemaGastos.Application.Features.Users.Queries;
using SistemaGastos.Application.Helpers;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Models;
using System.Security.Cryptography;
using System.Text;

namespace SistemaGastos.Application.Features.Users.Handlers;

public class UserManagementHandler(IApplicationDbContext context) :
    IRequestHandler<UpdateUserProfileCommand, bool>,
    IRequestHandler<ChangeUserPasswordCommand, bool>,
    IRequestHandler<ToggleUserStatusCommand, bool>,
    IRequestHandler<DeleteUserCommand, bool>,
    IRequestHandler<GetAllUsersQuery, List<Login>>,
    IRequestHandler<GetUserProfileQuery, Login?>
{
    // --- COMMANDS ---

    public async Task<bool> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Login.FindAsync([request.Id], cancellationToken);
        if (user == null) return false;

        user.Email = request.Email;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(ChangeUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Login.FindAsync([request.UserId], cancellationToken);
        if (user == null) return false;

        user.Password = SecurityHelper.HashPassword(request.NewPassword); // Método privado abajo
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(ToggleUserStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Login.FindAsync([request.UserId], cancellationToken);
        if (user == null) return false;

        user.Active = request.IsActive;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Login.FindAsync([request.UserId], cancellationToken);
        if (user == null) return false;

        context.Login.Remove(user);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    // --- QUERIES ---

    public async Task<List<Login>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        // Trae todos menos el usuario actual
        return await context.Login
            .Where(l => l.Username != request.CurrentUsername)
            .ToListAsync(cancellationToken);
    }

    public async Task<Login?> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        return await context.Login
            .FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);
    }
}