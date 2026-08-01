using System.Net;
using System.Text.Json;

namespace SistemaGastos.WebApp.Middleware;

// Heredamos de IMiddleware para poder inyectar servicios (como el Logger) fácilmente
public class GlobalExceptionHandlerMiddleware(ILogger<GlobalExceptionHandlerMiddleware> logger, IHostEnvironment env) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            // Intenta ejecutar la petición normal...
            await next(context);
        }
        catch (Exception ex)
        {
            // ¡Si algo explota, caemos aquí!
            logger.LogError(ex, "Error crítico no controlado: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        // 1. DETERMINAR CÓDIGO DE ESTADO (StatusCode)
        // Mapeamos tipos de Excepción -> HTTP Status Codes
        var statusCode = exception switch
        {
            // 404: Dato no encontrado (Ej: context.Account.FindAsync(...) devuelve null y lanzas esta ex)
            KeyNotFoundException => HttpStatusCode.NotFound,

            // 401: No autorizado (Ej: Token inválido o expirado)
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,

            // 400: Error de validación o argumentos inválidos (Ej: "El monto no puede ser negativo")
            ArgumentException or ArgumentNullException or InvalidOperationException
                => HttpStatusCode.BadRequest,

            FluentValidation.ValidationException => HttpStatusCode.BadRequest,

            // 500: Todo lo demás (Errores de BD, Nulos no controlados, etc.)
            _ => HttpStatusCode.InternalServerError
        };

        context.Response.StatusCode = (int)statusCode;

        // 2. DETERMINAR EL MENSAJE A MOSTRAR
        // Lógica: 
        // - Si es un error 500 en Producción -> Ocultamos el detalle (Seguridad).
        // - Si es un error 4xx (Validación/Negocio) -> Mostramos el mensaje real siempre (Feedback al usuario).
        // - Si estamos en Desarrollo -> Mostramos TODO siempre.

        string message = exception.Message;
        object? errors = null; // Para guardar la lista detallada de campos fallidos

        // Si es error de validación, extraemos los detalles
        if (exception is FluentValidation.ValidationException validationEx)
        {
            message = "Errores de validación detectados.";
            errors = validationEx.Errors.Select(e => new { Field = e.PropertyName, Error = e.ErrorMessage });
        }
        var exposeDetails = env.IsDevelopment() || statusCode != HttpStatusCode.InternalServerError;
        if (!exposeDetails)
        {
            message = "Error interno del servidor.";
        }

        var response = new
        {
            success = false,
            statusCode = (int)statusCode,
            message = message,
            errors = errors,
            exceptionType = exposeDetails ? exception.GetType().FullName : null,
            detail = exposeDetails
                ? exception.Message + " | " + exception.InnerException?.Message
                : null
        };

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}
