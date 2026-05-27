using SistemaGastos.Application.Interfaces;
using System.Security.Claims;

namespace SistemaGastos.WebApp.Services;

// Implementación concreta que sabe leer la Sesión HTTP
public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public int? UserId
    {
        get
        {
            var idString = httpContextAccessor.HttpContext?.User?.FindFirstValue("Id");

            if (int.TryParse(idString, out int id))
            {
                return id;
            }
            return null;
        }
    }

    public string? Username => httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);

    public bool IsAuthenticated => UserId.HasValue;
}