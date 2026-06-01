using MediatR;
using Microsoft.AspNetCore.Authorization;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Transactions.Commands;
using SistemaGastos.Application.Features.Transactions.Queries;

namespace SistemaGastos.Controllers;

[Authorize]
public class CreditCardTransactionController(IMediator mediator, ICurrentUserService currentUser) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = currentUser.UserId ?? 0;
        var viewModel = await mediator.Send(new GetCreditCardIndexQuery(userId));
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] List<CreditCardTransactionDto> dtos)
    {
        if (!currentUser.IsAuthenticated)
            return Json(new { success = false, message = "Sesión expirada" });

        if (!ModelState.IsValid || dtos.Count == 0)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Distinct();
            return Json(new { success = false, message = string.Join("<br/>", errors) });
        }

        try
        {
            await mediator.Send(new CreateBulkCreditCardTransactionsCommand(dtos));
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> Edit([FromBody] UpdateCreditCardTransactionDto dto)
    {
        if (!ModelState.IsValid) return Json(new { success = false, message = "Datos inválidos" });

        try
        {
            var success = await mediator.Send(new UpdateCreditCardTransactionCommand(dto));
            return success
                ? Json(new { success = true })
                : Json(new { success = false, message = "No se encontró la transacción" });
        }
        catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody] List<long> ids)
    {
        try
        {
            var success = await mediator.Send(new DeleteTransactionsCommand(ids));
            return Json(new { success = success });
        }
        catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
    }

    [HttpGet]
    public async Task<IActionResult> GetDatosTransaccionesTC(int limit = 10, int offset = 0, string keyword = "")
    {
        try
        {
            var result = await mediator.Send(new GetCreditCardTransactionsQuery(keyword, limit, offset));
            return Json(new { results = result.Results, total = result.Total });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetTransactionForm(int? id)
    {
        var userId = currentUser.UserId ?? 0;
        var result = await mediator.Send(new GetCreditCardTransactionFormQuery(userId, id));

        if (id.HasValue && result.Transaction == null) return NotFound();

        ViewBag.CreditCards = result.CreditCards
            .Select(a => new SelectListItem { Value = a.ID.ToString(), Text = a.Name })
            .ToList();

        ViewBag.Categories = new SelectList(result.Categories, "ID", "Name");
        ViewBag.Persons = result.Persons ?? new List<SistemaGastos.Application.DTOs.PersonDropdownDto>();

        var accountsList = new List<SelectListItem>();
        foreach (var currency in result.Accounts.Select(a => a.Currency).Distinct())
        {
            var group = new SelectListGroup { Name = currency };
            foreach (var acc in result.Accounts.Where(a => a.Currency == currency))
                accountsList.Add(new SelectListItem { Value = acc.ID.ToString(), Text = acc.Name, Group = group });
        }
        ViewBag.Accounts = accountsList;

        var model = result.Transaction ?? new CreditCardTransaction { TransactionDate = DateTime.Now };
        return PartialView("_CreditCardTransactionForm", model);
    }

    [HttpGet]
    public async Task<IActionResult> GetMultipleForm()
    {
        var userId = currentUser.UserId ?? 0;
        var result = await mediator.Send(new GetMultipleCreditCardFormQuery(userId));

        var accountsList = new List<SelectListItem>();
        foreach (var currency in result.Accounts.Select(a => a.Currency).Distinct())
        {
            var group = new SelectListGroup { Name = currency };
            foreach (var acc in result.Accounts.Where(a => a.Currency == currency))
                accountsList.Add(new SelectListItem { Value = acc.ID.ToString(), Text = acc.Name, Group = group });
        }
        ViewBag.Accounts = accountsList;
        ViewBag.Categories = new SelectList(result.Categories, "ID", "Name");
        ViewBag.DefaultDate = DateTime.Now.ToString("yyyy-MM-dd");

        return PartialView("_MultipleCreditCardTransactionForm");
    }

    [HttpGet]
    public async Task<IActionResult> GetTotals()
    {
        var userId = currentUser.UserId ?? 0;
        var result = await mediator.Send(new GetCreditCardTotalsQuery(userId));

        return Json(new
        {
            totalArs = result.TotalArs,
            totalUsd = result.TotalUsd,
            fijoArs = result.FixedArs,
            fijoUsd = result.FixedUsd,
            varArs = result.VariableArs,
            varUsd = result.VariableUsd
        });
    }
}
