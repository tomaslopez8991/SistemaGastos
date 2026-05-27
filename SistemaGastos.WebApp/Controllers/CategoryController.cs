using MediatR;
using Microsoft.AspNetCore.Authorization;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Categories.Commands;
using SistemaGastos.Application.Features.Categories.Queries;

namespace SistemaGastos.Controllers;

[Authorize]
public class CategoryController(IMediator mediator) : Controller
{
    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> GetCategoriesJson()
    {
        var categories = await mediator.Send(new GetCategoriesQuery());
        return Json(new { data = categories });
    }

    [HttpGet]
    public async Task<IActionResult> GetCategoryForm(int? id)
    {
        if (id.HasValue)
        {
            var category = await mediator.Send(new GetCategoryByIdQuery(id.Value));
            if (category == null) return NotFound();
            return PartialView("_CategoryForm", category);
        }
        return PartialView("_CategoryForm", new Category { Color = "#0d6efd", Icon = "fa-solid fa-tag" });
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveCategoryDto dto)
    {
        if (!ModelState.IsValid) return Json(new { success = false, message = "Datos inválidos" });
        try
        {
            await mediator.Send(new SaveCategoryCommand(dto));
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await mediator.Send(new DeleteCategoryCommand(id));
        return Json(new { success = true });
    }
}
