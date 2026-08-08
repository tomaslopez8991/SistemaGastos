using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Users.Commands;
using SistemaGastos.Application.Features.Users.Queries;
using SistemaGastos.Application.Helpers;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.Users.Handlers;

public class UserManagementHandler(
    IApplicationDbContext context,
    IEmailSender emailSender,
    IEmailTemplateHelper templateHelper) :
    IRequestHandler<UpdateUserProfileCommand, bool>,
    IRequestHandler<ChangeUserPasswordCommand, bool>,
    IRequestHandler<ToggleUserStatusCommand, bool>,
    IRequestHandler<SetUserRoleCommand, bool>,
    IRequestHandler<SetUserDeletedCommand, bool>,
    IRequestHandler<ResendUserConfirmationCommand, bool>,
    IRequestHandler<GetAllUsersQuery, List<AdminUserDto>>,
    IRequestHandler<GetUserProfileQuery, Login?>
{
    public async Task<bool> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Login.FindAsync([request.Id], cancellationToken);
        if (user == null || user.IsDeleted) return false;

        user.Email = request.Email.Trim().ToLowerInvariant();
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(ChangeUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Login.FindAsync([request.UserId], cancellationToken);
        if (user == null || user.IsDeleted) return false;

        user.Password = SecurityHelper.HashPassword(request.NewPassword);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(ToggleUserStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Login.FindAsync([request.UserId], cancellationToken);
        if (user == null || user.IsDeleted || request.IsActive && !user.EmailConfirmed) return false;

        user.Active = request.IsActive;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(SetUserRoleCommand request, CancellationToken cancellationToken)
    {
        var role = request.Role.Trim();
        if (role is not ("Admin" or "User")) return false;

        var user = await context.Login.FindAsync([request.UserId], cancellationToken);
        if (user == null || user.IsDeleted) return false;

        user.Role = role;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(SetUserDeletedCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Login.FindAsync([request.UserId], cancellationToken);
        if (user == null) return false;

        user.IsDeleted = request.IsDeleted;
        user.DeletedAt = request.IsDeleted ? DateTime.UtcNow : null;
        user.Active = !request.IsDeleted && user.EmailConfirmed;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(ResendUserConfirmationCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Login.FindAsync([request.UserId], cancellationToken);
        if (user == null || user.IsDeleted || user.EmailConfirmed) return false;

        var token = EmailConfirmationTokenHelper.CreateToken();
        var confirmationUrl = $"{request.OriginUrl.TrimEnd('/')}/Auth/ConfirmEmail?token={Uri.EscapeDataString(token)}";
        var body = await templateHelper.GetTemplateAsync("ConfirmarEmail.html", new()
        {
            ["NombreUsuario"] = System.Net.WebUtility.HtmlEncode(user.Username),
            ["LinkConfirmacion"] = confirmationUrl,
            ["Vencimiento"] = "24 horas"
        });
        await emailSender.SendEmailAsync(user.Email, "Confirmá tu cuenta en SistemaGastos", body);

        user.EmailConfirmationTokenHash = EmailConfirmationTokenHelper.Hash(token);
        user.EmailConfirmationTokenExpiry = DateTime.UtcNow.AddHours(24);
        user.ConfirmationEmailSentAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<AdminUserDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        return await context.Login
            .AsNoTracking()
            .Where(x => x.Username != request.CurrentUsername)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AdminUserDto(
                x.ID,
                x.Username,
                x.Email,
                x.Role ?? "User",
                x.Active,
                x.EmailConfirmed,
                x.CreatedAt,
                x.IsDeleted,
                x.DeletedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<Login?> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        return await context.Login
            .FirstOrDefaultAsync(x => x.Username == request.Username && !x.IsDeleted, cancellationToken);
    }
}
