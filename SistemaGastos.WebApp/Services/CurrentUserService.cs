using Microsoft.Extensions.Configuration;
using SistemaGastos.Application.Interfaces;
using System.Security.Claims;

namespace SistemaGastos.WebApp.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration) : ICurrentUserService
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

    public bool IsAdmin
    {
        get
        {
            var adminUsername = configuration["MiniProfiler:AdminUsername"];
            return !string.IsNullOrEmpty(adminUsername) &&
                   string.Equals(Username, adminUsername, StringComparison.OrdinalIgnoreCase);
        }
    }
}