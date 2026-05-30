using MediatR;
using Microsoft.AspNetCore.Authorization;
using SistemaGastos.Application.Features.Notifications.Queries;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Controllers;

[Authorize]
public class NotificationsController(IMediator mediator, ICurrentUserService currentUser) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = currentUser.UserId ?? 0;
        if (userId == 0) return RedirectToAction("Login", "Auth");

        var notifications = await mediator.Send(new GetAllNotificationsQuery(userId));
        return View(notifications);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = currentUser.UserId ?? 0;
        if (userId == 0) return Unauthorized();

        var notifications = await mediator.Send(new GetAllNotificationsQuery(userId));
        return Json(new { count = notifications.Count, notifications });
    }
}
