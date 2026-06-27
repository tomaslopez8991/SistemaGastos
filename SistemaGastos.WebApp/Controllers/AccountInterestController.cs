using MediatR;
using Microsoft.AspNetCore.Authorization;
using SistemaGastos.Application.Features.AccountInterest.Commands;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Application.Wrappers;

namespace SistemaGastos.Controllers;

[Authorize]
public class AccountInterestController(IMediator mediator, ICurrentUserService currentUser) : Controller
{
    [HttpPost]
    public async Task<IActionResult> Recalculate()
    {
        if (currentUser.UserId is not int userId)
            return Unauthorized();

        await mediator.Send(new RecalculateAccountInterestCommand(userId));
        return Ok(new Response<bool>(true, "Intereses recalculados correctamente."));
    }

    [HttpGet]
    public async Task<IActionResult> GetTotalAccrued([FromServices] IAccountInterestService accountInterestService)
    {
        if (currentUser.UserId is not int userId)
            return Unauthorized();

        var total = await accountInterestService.GetTotalAccruedInterestAsync(userId);
        return Ok(new Response<decimal>(total));
    }
}
