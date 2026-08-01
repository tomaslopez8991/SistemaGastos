using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Accounts.Commands;
using SistemaGastos.Application.Features.Accounts.Queries;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Application.Wrappers;
using SistemaGastos.Domain.Enums;

namespace SistemaGastos.Controllers;

[Authorize]
public class AccountController(IMediator mediator, ICurrentUserService currentUser) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetAccountForm(int? id)
    {
        if (id.HasValue)
        {
            var accountDto = await mediator.Send(new GetAccountByIdQuery(id.Value));

            if (accountDto == null) return NotFound();

            return PartialView("_AccountForm", accountDto);
        }

        return PartialView("_AccountForm", new AccountDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountDto dto)
    {
        var id = await mediator.Send(new CreateAccountCommand(dto));
        return Ok(new Response<int>(id, "Cuenta creada exitosamente."));
    }

    [HttpPut]
    public async Task<IActionResult> Edit([FromBody] UpdateAccountDto dto)
    {
        await mediator.Send(new UpdateAccountCommand(dto));
        return Ok(new Response<bool>(true, "Cuenta actualizada correctamente."));
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        await mediator.Send(new DeleteAccountCommand(id));
        return Ok(new Response<bool>(true, "Cuenta eliminada correctamente."));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var accounts = await mediator.Send(new GetAccountsQuery());
        return Ok(new Response<List<AccountDto>>(accounts));
    }

    [HttpGet]
    public async Task<IActionResult> GetAccountTotals()
    {
        var totals = await mediator.Send(new GetAccountTotalsQuery());
        return Ok(new Response<List<AccountTotalDto>>(totals));
    }

    [HttpGet]
    public async Task<IActionResult> GetTransferForm(int originId)
    {
        var origin = await mediator.Send(new GetAccountByIdQuery(originId));
        if (origin == null) return NotFound();

        var destinations = await mediator.Send(new GetTransferTargetsQuery(originId));

        ViewBag.DestinationAccounts = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(destinations, "Id", "DisplayName");
        ViewBag.OriginName = origin.Name;

        ViewBag.AvailableBalance = origin.Balance;

        var model = new TransferDto(originId, 0, 0, DateTime.Now);

        return PartialView("_TransferForm", model);
    }

    [HttpPost]
    public async Task<IActionResult> ProcessTransfer([FromBody] TransferDto dto)
    {
        var result = await mediator.Send(new TransferMoneyCommand(dto));
        return Ok(new Response<bool>(result, "Saldo transferido correctamente."));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayCreditCard([FromBody] PayCreditCardRequest req)
    {
        var userId = currentUser.UserId ?? 0;
        var command = new PayCreditCardCommand(
            req.TcAccountId,
            req.SourceAccountId,
            req.Amount,
            req.PaymentDate,
            userId,
            req.FixedExpenseId);
        var result = await mediator.Send(command);
        return Ok(Response<PaymentResultDto>.Ok(result, result.Message));
    }
}

public record PayCreditCardRequest(
    int TcAccountId,
    int SourceAccountId,
    decimal Amount,
    DateTime PaymentDate,
    int? FixedExpenseId);
