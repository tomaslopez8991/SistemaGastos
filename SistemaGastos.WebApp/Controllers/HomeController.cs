using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGastos.Application.Features.AccountInterest.Commands;
using SistemaGastos.Application.Features.Dashboard.Queries;
using SistemaGastos.WebApp.Services;

namespace SistemaGastos.WebApp.Controllers;

[Authorize]
public class HomeController(IMediator mediator, ICurrentUserService currentUser) : Controller
{
    public async Task<IActionResult> Index()
    {
        var username = currentUser.Username;

        if (string.IsNullOrEmpty(username))
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login", "Auth");
        }

        var model = await mediator.Send(new GetDashboardMetricsQuery(username));

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> RecalcularIntereses()
    {
        if (currentUser.UserId.HasValue)
        {
            await mediator.Send(new RecalculateAccountInterestCommand(currentUser.UserId.Value));
            TempData["SuccessMessage"] = "Intereses recalculados correctamente.";
        }

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Privacy() => View();
    public IActionResult Error() => View();
}