using AutoMapper;
using Humanizer;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Transactions.Commands;
using SistemaGastos.Application.Features.Transactions.Queries;
using SistemaGastos.WebApp.Models.ViewModels;
using System.Collections.Generic;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace SistemaGastos.Controllers;

[Authorize]
public class CreditCardTransactionController(ApplicationDbContext context, IMediator mediator, ICurrentUserService currentUser) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var accounts = await context.Account.OrderBy(a => a.Name).ToListAsync();
        var categories = await context.Category.OrderBy(c => c.Name).ToListAsync();

        var viewModel = new CreditCardTransactionIndexVM
        {
            Accounts = accounts,
            Categories = categories
        };

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
            // (Idealmente esto lo maneja un Middleware global, pero por ahora está bien aquí)
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
            var query = new GetCreditCardTransactionsQuery(keyword, limit, offset);
            var result = await mediator.Send(query);

            // Devolvemos el JSON con la estructura exacta que Grid.js Server-Side espera
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
        var username = currentUser.Username;

        var tarjetas = await context.Account
            .Where(a => a.Login.Username == username && a.Type == Domain.Enums.AccountType.TarjetaCredito)
            .OrderBy(a => a.Name)
            .Select(a => new SelectListItem { Value = a.ID.ToString(), Text = a.Name })
            .ToListAsync();

        ViewBag.CreditCards = tarjetas;

        // Cargar categorías para el Select
        var categories = context.Category.ToList();
        ViewBag.Categories = new SelectList(categories, "ID", "Name");

        var accountsRaw = await context.Account
            .Where(a => a.Login.Username == username && a.Type == Domain.Enums.AccountType.TarjetaCredito)
            .OrderBy(a => a.Currency).ThenBy(a => a.Name)
            .ToListAsync();

        var accountsList = new List<SelectListItem>();
        var currencies = accountsRaw.Select(a => a.Currency).Distinct().ToList();

        foreach (var currency in currencies)
        {
            var group = new SelectListGroup { Name = currency };
            foreach (var acc in accountsRaw.Where(a => a.Currency == currency))
            {
                accountsList.Add(new SelectListItem
                {
                    Value = acc.ID.ToString(),
                    Text = acc.Name,
                    Group = group
                });
            }
        }
        ViewBag.Accounts = accountsList;

        if (id.HasValue)
        {
            // Es EDICIÓN: Buscamos el gasto
            var transaction = await context.CreditCardTransaction.FindAsync(id);
            if (transaction == null) return NotFound();
            return PartialView("_CreditCardTransactionForm", transaction);
        }

        // Es CREACIÓN: Retornamos modelo vacío con fecha hoy
        return PartialView("_CreditCardTransactionForm", new CreditCardTransaction { TransactionDate = DateTime.Now });
    }

    [HttpGet]
    public IActionResult GetMultipleForm()
    {
        var username = currentUser.Username;

        var accountsRaw = context.Account
            .Where(a => a.Login.Username == username && a.Type == Domain.Enums.AccountType.TarjetaCredito)
            .OrderBy(a => a.Currency).ThenBy(a => a.Name)
            .ToList();

        var accountsList = new List<SelectListItem>();
        var currencies = accountsRaw.Select(a => a.Currency).Distinct().ToList();

        foreach (var currency in currencies)
        {
            var group = new SelectListGroup { Name = currency };
            foreach (var acc in accountsRaw.Where(a => a.Currency == currency))
            {
                accountsList.Add(new SelectListItem
                {
                    Value = acc.ID.ToString(),
                    Text = acc.Name,
                    Group = group
                });
            }
        }

        ViewBag.Accounts = accountsList;
        ViewBag.Categories = new SelectList(
            context.Category.Where(x => x.Type == "Gasto"), "ID", "Name");
        ViewBag.DefaultDate = DateTime.Now.ToString("yyyy-MM-dd");

        return PartialView("_MultipleCreditCardTransactionForm");
    }

    [HttpGet]
    public IActionResult GetTotals()
    {
        var username = currentUser.Username;

        // Obtenemos todos los gastos del usuario actual
        var query = context.CreditCardTransaction
            .Where(t => t.Account.Login.Username == username);

        // Calculamos totales por moneda
        var totalARS = query.Where(t => t.Account.Currency == "ARS").Sum(t => t.Amount);
        var totalUSD = query.Where(t => t.Account.Currency == "USD").Sum(t => t.Amount);

        // Calculamos Fijos vs Variables (separados por moneda para conversión precisa)
        var fijoARS = query.Where(t => t.Fixed && t.Account.Currency == "ARS").Sum(t => t.Amount);
        var fijoUSD = query.Where(t => t.Fixed && t.Account.Currency == "USD").Sum(t => t.Amount);

        var varARS = query.Where(t => !t.Fixed && t.Account.Currency == "ARS").Sum(t => t.Amount);
        var varUSD = query.Where(t => !t.Fixed && t.Account.Currency == "USD").Sum(t => t.Amount);

        return Json(new
        {
            totalArs = totalARS,
            totalUsd = totalUSD,
            fijoArs = fijoARS,
            fijoUsd = fijoUSD,
            varArs = varARS,
            varUsd = varUSD
        });
    }
}
