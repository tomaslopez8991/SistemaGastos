using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.FixedExpense.Commands;
using SistemaGastos.Application.Features.FixedExpense.Queries;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Application.Wrappers;
using SistemaGastos.WebApp.Models;

namespace SistemaGastos.Controllers;

[Authorize]
[Route("FixedExpense")]
public class FixedExpenseController(ApplicationDbContext context, IMediator mediator, ICurrentUserService currentUser) : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet("GetList")]
    public async Task<ActionResult<Response<List<FixedExpenseDto>>>> GetList()
    {
        int userID = currentUser.UserId ?? 0;
        var expenses = await mediator.Send(new GetAllFixedExpensesQuery(userID));
        return Ok(Response<List<FixedExpenseDto>>.Ok(expenses));
    }

    [HttpGet("GetForm/{id?}")]
    public async Task<IActionResult> GetForm(int? id)
    {
        int userID = currentUser.UserId ?? 0;
        var formData = await mediator.Send(new GetFixedExpenseFormQuery(userID, id));
        return PartialView("_FixedExpenseForm", formData);
    }

    [HttpPost("Save")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Response<int>>> Save([FromBody] FixedExpenseDto dto)
    {
        int userID = currentUser.UserId ?? 0;
        var command = new SaveFixedExpenseCommand(dto, userID);
        var id = await mediator.Send(command);

        if (id > 0)
        {
            var message = dto.ID > 0 ? "Gasto actualizado correctamente" : "Gasto creado correctamente";
            return Ok(Response<int>.Ok(id, message));
        }

        return Ok(Response<int>.Fail("No se pudo guardar el gasto"));
    }

    [HttpPost("PayNow")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Response<PaymentResultDto>>> PayNow([FromBody] int id)
    {
        int userID = currentUser.UserId ?? 0;
        var command = new ProcessFixedExpensePaymentCommand(id, userID);
        var result = await mediator.Send(command);

        return Ok(Response<PaymentResultDto>.Ok(result, result.Message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Response<bool>>> ToggleActive([FromBody] int id)
    {
        int userID = currentUser.UserId ?? 0;
        var command = new ToggleFixedExpenseActiveCommand(id, userID);
        var result = await mediator.Send(command);

        if (result)
        {
            return Ok(Response<bool>.Ok(true, "Estado actualizado correctamente"));
        }

        return Ok(Response<bool>.Fail("No se pudo actualizar el estado"));
    }

    [HttpDelete("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Response<bool>>> Delete(int id)
    {
        int userID = currentUser.UserId ?? 0;
        var command = new DeleteFixedExpenseCommand(id, userID);
        var result = await mediator.Send(command);

        if (result)
        {
            return Ok(Response<bool>.Ok(true, "Gasto eliminado correctamente"));
        }

        return Ok(Response<bool>.Fail("No se pudo eliminar el gasto"));
    }
}
