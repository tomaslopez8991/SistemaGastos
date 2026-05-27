using Microsoft.AspNetCore.Authorization;

namespace SistemaGastos.Controllers;

[Authorize]
public class CategoryController(ApplicationDbContext context) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetCategoriesJson()
    {
        // Devuelve la lista limpia para renderizar en el front
        var categories = await context.Category
            .Select(c => new
            {
                id = c.ID,
                name = c.Name,
                type = c.Type, // "Gasto" o "Ingreso"
                icon = c.Icon, // Clase de FontAwesome (ej: "fa-solid fa-car")
                color = c.Color, // Hex (ej: "#ff0000")
                description = c.Description
            })
            .OrderBy(c => c.name)
            .ToListAsync();

        return Json(new { data = categories });
    }

    [HttpGet]
    public async Task<IActionResult> GetCategoryForm(int? id)
    {
        if (id.HasValue)
        {
            var category = await context.Category.FindAsync(id);
            if (category == null) return NotFound();
            return PartialView("_CategoryForm", category);
        }
        // Nueva categoría con valores por defecto
        return PartialView("_CategoryForm", new Category { Color = "#0d6efd", Icon = "fa-solid fa-tag" });
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] Category category)
    {
        if (!ModelState.IsValid) return Json(new { success = false, message = "Datos inválidos" });

        try
        {
            if (category.ID > 0)
            {
                var existing = await context.Category.FindAsync(category.ID);
                if (existing == null) return NotFound();

                existing.Name = category.Name;
                existing.Type = category.Type;
                existing.Description = category.Description;
                existing.Icon = category.Icon;
                existing.Color = category.Color;

                context.Category.Update(existing);
            }
            else
            {
                context.Category.Add(category);
            }

            await context.SaveChangesAsync();
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
        // Validación opcional: No borrar si tiene transacciones
        /* if(context.Transaction.Any(t => t.CategoryID == id)) 
            return Json(new { success = false, message = "No se puede borrar porque tiene movimientos asociados." });
        */

        var cat = await context.Category.FindAsync(id);
        if (cat != null)
        {
            context.Category.Remove(cat);
            await context.SaveChangesAsync();
            return Json(new { success = true });
        }
        return Json(new { success = false, message = "No encontrado" });
    }
}
