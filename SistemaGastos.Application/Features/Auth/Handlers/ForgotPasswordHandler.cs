using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.Auth.Commands;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.Auth.Handlers;

public class ForgotPasswordHandler(
    IApplicationDbContext context,
    IEmailSender emailSender,
    IEmailTemplateHelper templateHelper
    ) : IRequestHandler<ForgotPasswordCommand, bool>
{
    public async Task<bool> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var usuario = await context.Login
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (usuario == null)
        {
            await Task.Delay(500, cancellationToken);
            return true;
        }

        var token = Guid.NewGuid().ToString();
        
        usuario.ResetToken = token;
        usuario.ResetTokenExpiry = DateTime.Now.AddMinutes(10);

        context.Login.Update(usuario);
        await context.SaveChangesAsync(cancellationToken);

        var link = $"{request.OriginUrl}/Auth/ResetPassword?token={token}";

        try
        {
            var replacements = new Dictionary<string, string>
            {
                { "NombreUsuario", usuario.Username ?? "Usuario" },
                { "LinkRecuperacion", link }
            };

            var cuerpoCorreo = await templateHelper.GetTemplateAsync("RecuperarPassword.html", replacements);
            
            await emailSender.SendEmailAsync(request.Email, "Restablecer Contraseña", cuerpoCorreo);
        }
        catch
        {
            // Loguear error de email aquí (Serilog, etc.)
            // No lanzamos excepción para no romper el flujo visual del usuario
        }

        return true;
    }
}