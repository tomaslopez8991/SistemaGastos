using Microsoft.AspNetCore.Hosting;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Infraestructure.Services;

public class EmailTemplateHelper(IWebHostEnvironment env) : IEmailTemplateHelper
{
    public async Task<string> GetTemplateAsync(string templateName, Dictionary<string, string> replacements)
    {
        // Validación de seguridad: Verificar que WebRootPath no sea nulo
        if (string.IsNullOrEmpty(env.WebRootPath))
        {
            throw new InvalidOperationException("El entorno web no tiene configurado un WebRootPath (wwwroot).");
        }

        // 1. Construir la ruta
        var path = Path.Combine(env.WebRootPath, "templates", templateName);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"La plantilla no existe en: {path}");
        }

        // 2. Leer contenido
        var templateContent = await File.ReadAllTextAsync(path);

        // 3. Reemplazar placeholders (Optimización con Aggregate para ser más funcional)
        return replacements.Aggregate(templateContent, (current, item) =>
            current.Replace($"{{{{{item.Key}}}}}", item.Value));
    }
}