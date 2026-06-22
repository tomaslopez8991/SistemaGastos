using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Persons.Commands;
using SistemaGastos.Application.Features.Persons.Queries;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Application.Wrappers;
using SistemaGastos.Domain.Enums;

namespace SistemaGastos.Controllers;

[Authorize]
[Route("Person")]
public class PersonController(IMediator mediator, ICurrentUserService currentUser, IApplicationDbContext context) : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet("GetList")]
    public async Task<ActionResult<Response<List<PersonDto>>>> GetList()
    {
        var userID = currentUser.UserId ?? 0;
        var persons = await mediator.Send(new GetPersonsQuery(userID));
        return Ok(Response<List<PersonDto>>.Ok(persons));
    }

    [HttpPost("Save")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Response<int>>> Save([FromBody] PersonDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Ok(Response<int>.Fail("El nombre es requerido"));

        var userID = currentUser.UserId ?? 0;
        var id = await mediator.Send(new SavePersonCommand(userID, dto.ID, dto.Name, dto.CollectionDay, dto.DiscountAmount, dto.CollectionFrom));

        if (id > 0)
        {
            var msg = dto.ID > 0 ? "Persona actualizada correctamente" : "Persona creada correctamente";
            return Ok(Response<int>.Ok(id, msg));
        }

        return Ok(Response<int>.Fail("No se pudo guardar la persona"));
    }

    [HttpDelete("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Response<bool>>> Delete(int id)
    {
        var userID = currentUser.UserId ?? 0;
        var result = await mediator.Send(new DeletePersonCommand(id, userID));

        if (result)
            return Ok(Response<bool>.Ok(true, "Persona eliminada correctamente"));

        return Ok(Response<bool>.Fail("No se pudo eliminar la persona"));
    }

    [HttpGet("GetAccounts")]
    public async Task<ActionResult<Response<List<PersonAccountDto>>>> GetAccounts(int? year, int? month)
    {
        var userID = currentUser.UserId ?? 0;
        var now = DateTime.Now;
        var accounts = await mediator.Send(new GetPersonAccountsQuery(
            userID,
            year ?? now.Year,
            month ?? now.Month));
        return Ok(Response<List<PersonAccountDto>>.Ok(accounts));
    }

    [HttpGet("GetDropdown")]
    public async Task<ActionResult<Response<List<PersonDto>>>> GetDropdown()
    {
        var userID = currentUser.UserId ?? 0;
        var persons = await mediator.Send(new GetPersonsQuery(userID));
        return Ok(Response<List<PersonDto>>.Ok(persons));
    }

    /// <summary>Cuentas disponibles para acreditar un cobro (excluye TC).</summary>
    [HttpGet("GetPaymentAccounts")]
    public async Task<ActionResult<Response<List<object>>>> GetPaymentAccounts()
    {
        var userID = currentUser.UserId ?? 0;
        var accounts = await context.Account
            .Where(a => a.UserID == userID && a.Type != AccountType.TarjetaCredito)
            .Select(a => new { id = a.ID, name = a.Name, currency = a.Currency, balance = a.Balance })
            .ToListAsync();
        return Ok(Response<object>.Ok(accounts));
    }

    [HttpPost("RegisterPayment")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Response<bool>>> RegisterPayment([FromBody] PersonPaymentRequest req)
    {
        var userID = currentUser.UserId ?? 0;
        var ok = await mediator.Send(new RegisterPersonPaymentCommand(req.PersonID, userID, req.AccountID, req.Amount));
        return ok
            ? Ok(Response<bool>.Ok(true, "Cobro registrado correctamente"))
            : Ok(Response<bool>.Fail("No se pudo registrar el cobro"));
    }
}

public class PersonPaymentRequest
{
    public int PersonID { get; set; }
    public int AccountID { get; set; }
    public decimal Amount { get; set; }
}
