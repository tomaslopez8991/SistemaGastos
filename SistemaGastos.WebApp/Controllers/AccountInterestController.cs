using MediatR;
using Microsoft.AspNetCore.Authorization;
using SistemaGastos.Application.Features.AccountInterest.Commands;
using SistemaGastos.Application.Features.AccountInterest.Queries;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Application.Wrappers;

namespace SistemaGastos.Controllers;

[Authorize]
public class AccountInterestController(IMediator mediator, ICurrentUserService currentUser) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (currentUser.UserId is not int userId) return Unauthorized();
        var dto = await mediator.Send(new GetAccountInterestPageQuery(userId));
        return View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> SaveSetting([FromBody] UpsertAccountInterestSettingCommand command)
    {
        if (currentUser.UserId is not int userId) return Unauthorized();
        var cmd = command with { UserID = userId };
        await mediator.Send(cmd);
        return Ok(new Response<bool>(true, "Configuración guardada."));
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        if (currentUser.UserId is not int userId) return Unauthorized();
        var enabled = await mediator.Send(new ToggleAccountInterestSettingCommand(id, userId));
        return Ok(new Response<bool>(enabled, enabled ? "Activado." : "Desactivado."));
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteSetting(int id)
    {
        if (currentUser.UserId is not int userId) return Unauthorized();
        await mediator.Send(new DeleteAccountInterestSettingCommand(id, userId));
        return Ok(new Response<bool>(true, "Configuración eliminada."));
    }

    [HttpPost]
    public async Task<IActionResult> Recalculate()
    {
        if (currentUser.UserId is not int userId) return Unauthorized();
        await mediator.Send(new RecalculateAccountInterestCommand(userId));
        return Ok(new Response<bool>(true, "Intereses recalculados correctamente."));
    }

    [HttpGet]
    public async Task<IActionResult> GetTotalAccrued([FromServices] IAccountInterestService accountInterestService)
    {
        if (currentUser.UserId is not int userId) return Unauthorized();
        var total = await accountInterestService.GetTotalAccruedInterestAsync(userId);
        return Ok(new Response<decimal>(total));
    }
}
