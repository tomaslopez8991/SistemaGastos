using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.FixedIncome.Commands;
using SistemaGastos.Application.Features.FixedIncome.Queries;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Application.Wrappers;

namespace SistemaGastos.Controllers;

[Authorize]
[Route("FixedIncome")]
public class FixedIncomeController(IMediator mediator, ICurrentUserService currentUser) : Controller
{
    [HttpGet("GetList")]
    public async Task<ActionResult<Response<List<FixedIncomeDto>>>> GetList(int? year, int? month)
    {
        int userID = currentUser.UserId ?? 0;
        var now = DateTime.Now;
        var result = await mediator.Send(new GetAllFixedIncomesQuery(userID, year ?? now.Year, month ?? now.Month));
        return Ok(Response<List<FixedIncomeDto>>.Ok(result));
    }

    [HttpGet("GetForm/{id?}")]
    public async Task<IActionResult> GetForm(int? id)
    {
        int userID = currentUser.UserId ?? 0;
        var formData = await mediator.Send(new GetFixedIncomeFormQuery(userID, id));
        return PartialView("~/Views/FixedIncome/_FixedIncomeForm.cshtml", formData);
    }

    [HttpPost("Save")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Response<int>>> Save([FromBody] FixedIncomeDto dto)
    {
        int userID = currentUser.UserId ?? 0;
        var id = await mediator.Send(new SaveFixedIncomeCommand(dto, userID));
        if (id > 0)
        {
            var msg = dto.ID > 0 ? "Ingreso actualizado correctamente" : "Ingreso creado correctamente";
            return Ok(Response<int>.Ok(id, msg));
        }
        return Ok(Response<int>.Fail("No se pudo guardar el ingreso"));
    }

    [HttpPost("ReceiveNow")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Response<ReceiptResultDto>>> ReceiveNow([FromBody] int id)
    {
        int userID = currentUser.UserId ?? 0;
        var result = await mediator.Send(new ProcessFixedIncomeReceiptCommand(id, userID));
        return Ok(Response<ReceiptResultDto>.Ok(result, result.Message));
    }

    [HttpPost("ToggleActive")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Response<bool>>> ToggleActive([FromBody] ToggleActiveRequest request)
    {
        int userID = currentUser.UserId ?? 0;
        var activateFrom = (request.Year.HasValue && request.Month.HasValue)
            ? new DateTime(request.Year.Value, request.Month.Value, 1)
            : (DateTime?)null;
        var ok = await mediator.Send(new ToggleFixedIncomeActiveCommand(request.ID, userID, activateFrom));
        return ok
            ? Ok(Response<bool>.Ok(true, "Estado actualizado"))
            : Ok(Response<bool>.Fail("No se pudo actualizar"));
    }

    [HttpDelete("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Response<bool>>> Delete(int id)
    {
        int userID = currentUser.UserId ?? 0;
        var ok = await mediator.Send(new DeleteFixedIncomeCommand(id, userID));
        return ok
            ? Ok(Response<bool>.Ok(true, "Ingreso eliminado correctamente"))
            : Ok(Response<bool>.Fail("No se pudo eliminar el ingreso"));
    }
}

public class ToggleActiveRequest
{
    public int ID { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
}
