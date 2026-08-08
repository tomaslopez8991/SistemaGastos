using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.Auth.Commands;
using SistemaGastos.Application.Helpers;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.Auth.Handlers;

public class ConfirmEmailHandler(IApplicationDbContext context)
    : IRequestHandler<ConfirmEmailCommand, bool>
{
    public async Task<bool> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token)) return false;

        var tokenHash = EmailConfirmationTokenHelper.Hash(request.Token);
        var user = await context.Login.FirstOrDefaultAsync(
            x => x.EmailConfirmationTokenHash == tokenHash,
            cancellationToken);

        if (user == null || user.IsDeleted || user.EmailConfirmed || user.EmailConfirmationTokenExpiry < DateTime.UtcNow)
            return false;

        user.EmailConfirmed = true;
        user.Active = true;
        user.EmailConfirmationTokenHash = null;
        user.EmailConfirmationTokenExpiry = null;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class ResendConfirmationEmailHandler(
    IApplicationDbContext context,
    IEmailSender emailSender,
    IEmailTemplateHelper templateHelper)
    : IRequestHandler<ResendConfirmationEmailCommand, bool>
{
    public async Task<bool> Handle(ResendConfirmationEmailCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await context.Login.FirstOrDefaultAsync(
            x => x.Email.ToLower() == normalizedEmail,
            cancellationToken);

        // Respuesta neutra para evitar enumeración de usuarios.
        if (user == null || user.IsDeleted || user.EmailConfirmed) return true;
        if (user.ConfirmationEmailSentAt > DateTime.UtcNow.AddMinutes(-2)) return true;

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
}
