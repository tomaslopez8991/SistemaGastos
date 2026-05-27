using MediatR;
using Microsoft.AspNetCore.Authorization;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Budgets.Commands;
using SistemaGastos.Application.Features.Budgets.Queries;

namespace SistemaGastos.Controllers;

[Authorize]
public class BudgetController(IMediator mediator, ICurrentUserService currentUser) : Controller
{
    public async Task<IActionResult> GetBudgetTab(int month, int year)
    {
        var userId = currentUser.UserId ?? 0;
        if (userId == 0) return RedirectToAction("Index", "Login");

        ViewBag.CurrentMonth = month;
        ViewBag.CurrentYear = year;
        ViewBag.DateLabel = new DateTime(year, month, 1).ToString("MMMM yyyy").ToUpper();

        var model = await mediator.Send(new GetBudgetTabQuery(userId, month, year));
        return PartialView("_BudgetCards", model);
    }

    [HttpGet]
    public async Task<IActionResult> GetBudgetForm(int? id, int? month, int? year)
    {
        int m = month ?? DateTime.Now.Month;
        int y = year ?? DateTime.Now.Year;

        var result = await mediator.Send(new GetBudgetFormQuery(id, m, y));

        ViewBag.Categories = result.Categories
            .Select(c => new SelectListItem { Value = c.ID.ToString(), Text = c.Name })
            .ToList();

        var budgetModel = result.BudgetData ?? new Budget { PeriodMonth = result.Month, PeriodYear = result.Year };
        return PartialView("_BudgetForm", budgetModel);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveBudgetDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            return Json(new { success = false, message = "Datos inválidos: " + errors });
        }

        try
        {
            var userId = currentUser.UserId ?? 0;
            if (userId == 0) return Json(new { success = false, message = "La sesión ha expirado." });

            await mediator.Send(new SaveBudgetCommand(dto, userId));
            return Json(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch
        {
            return Json(new { success = false, message = "Error interno al guardar el presupuesto." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete([FromBody] int id)
    {
        await mediator.Send(new DeleteBudgetCommand(id));
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> CloneFromPreviousMonth(int targetMonth, int targetYear)
    {
        var userId = currentUser.UserId ?? 0;
        if (userId == 0) return Json(new { success = false, message = "La sesión ha expirado." });

        var result = await mediator.Send(new CloneFromPreviousMonthCommand(targetMonth, targetYear, userId));
        return result
            ? Json(new { success = true })
            : Json(new { success = false, message = "No hay nada que copiar del mes anterior." });
    }

    [HttpGet]
    public async Task<IActionResult> GetDetails(int id)
    {
        var result = await mediator.Send(new GetBudgetDetailsQuery(id));
        if (result == null) return NotFound();

        ViewBag.CategoryName = result.CategoryName;
        ViewBag.Period = result.Period;

        return PartialView("_BudgetDetails", result.Items);
    }
}
