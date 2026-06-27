using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGastos.Application.Features.SystemAlerts;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Controllers;

[Authorize]
public class SystemAlertsController(IMediator mediator, ICurrentUserService currentUser) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        if (!currentUser.IsAdmin) return Forbid();
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs(int page = 1, int pageSize = 25)
    {
        if (!currentUser.IsAdmin) return Forbid();
        var (items, total) = await mediator.Send(new GetPerformanceLogsQuery(page, pageSize));
        return Json(new { results = items, total });
    }

    [HttpDelete]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearLogs([FromServices] SistemaGastos.Application.Interfaces.IApplicationDbContext db)
    {
        if (!currentUser.IsAdmin) return Forbid();
        db.PerformanceLog.RemoveRange(db.PerformanceLog);
        await db.SaveChangesAsync(CancellationToken.None);
        return Ok();
    }
}
