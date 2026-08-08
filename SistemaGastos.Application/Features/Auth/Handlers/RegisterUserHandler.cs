using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.Auth.Commands;
using SistemaGastos.Application.Helpers;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.Auth.Handlers;

public class RegisterUserHandler(
    IApplicationDbContext context,
    IEmailSender emailSender,
    IEmailTemplateHelper templateHelper)
    : IRequestHandler<RegisterUserCommand, bool>
{
    public async Task<bool> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        var exists = await context.Login.AnyAsync(
            u => !u.IsDeleted && (u.Username == username || u.Email.ToLower() == email),
            cancellationToken);

        if (exists)
            throw new InvalidOperationException("No se pudo completar el registro con esos datos.");

        var token = EmailConfirmationTokenHelper.CreateToken();
        var now = DateTime.UtcNow;
        var newUser = new Login
        {
            Username = username,
            Email = email,
            Password = SecurityHelper.HashPassword(request.Password),
            Role = "User",
            Active = false,
            EmailConfirmed = false,
            EmailConfirmationTokenHash = EmailConfirmationTokenHelper.Hash(token),
            EmailConfirmationTokenExpiry = now.AddHours(24),
            ConfirmationEmailSentAt = now,
            CreatedAt = now
        };

        context.Login.Add(newUser);
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            var confirmationUrl = $"{request.OriginUrl.TrimEnd('/')}/Auth/ConfirmEmail?token={Uri.EscapeDataString(token)}";
            var body = await templateHelper.GetTemplateAsync("ConfirmarEmail.html", new()
            {
                ["NombreUsuario"] = System.Net.WebUtility.HtmlEncode(newUser.Username),
                ["LinkConfirmacion"] = confirmationUrl,
                ["Vencimiento"] = "24 horas"
            });
            await emailSender.SendEmailAsync(newUser.Email, "Confirmá tu cuenta en SistemaGastos", body);
        }
        catch
        {
            context.Login.Remove(newUser);
            await context.SaveChangesAsync(cancellationToken);
            throw;
        }

        return true;
    }
}
