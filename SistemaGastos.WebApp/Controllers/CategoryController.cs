using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Categories.Commands;
using SistemaGastos.Application.Features.Categories.Queries;

namespace SistemaGastos.Controllers;

[Authorize]
public class CategoryController(IMediator mediator, ILogger<CategoryController> logger) : Controller
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

        return PartialView("_CategoryForm", new Category
        {
            Color = "#0D6EFD",
            Icon = "fa-solid fa-tag",
            Type = "Gasto"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([FromBody] SaveCategoryDto dto)
    {
        if (!ModelState.IsValid)
        {
            var message = ModelState.Values.SelectMany(x => x.Errors).FirstOrDefault()?.ErrorMessage
                ?? "Revisá los datos ingresados.";
            return BadRequest(new { success = false, message });
        }

        try
        {
            var id = await mediator.Send(new SaveCategoryCommand(dto));
            return Ok(new { success = true, id });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Errors.FirstOrDefault()?.ErrorMessage ?? "Revisá los datos ingresados."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al guardar la categoría {CategoryId}", dto.ID);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { success = false, message = "No pudimos guardar la categoría. Intentá nuevamente." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await mediator.Send(new DeleteCategoryCommand(id));
            return Ok(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al eliminar la categoría {CategoryId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { success = false, message = "No pudimos eliminar la categoría. Intentá nuevamente." });
        }
    }
}
