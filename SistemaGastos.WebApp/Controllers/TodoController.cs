using Microsoft.AspNetCore.Authorization;

namespace SistemaGastos.Controllers;

[Authorize]
public class TodoController(ApplicationDbContext context, ICurrentUserService currentUser) : Controller
{
    public IActionResult Index()
    {
        if (!currentUser.IsAuthenticated)
            return RedirectToAction("Login", "Access");

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetTasksJson()
    {
        var username = currentUser.Username;
        var user = await context.Login.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return Unauthorized();

        var tasks = await context.TodoTask
            .Where(t => t.UserID == user.ID)
            .OrderBy(t => t.IsCompleted) // Primero pendientes
            .ThenBy(t => t.DueDate)      // Luego por fecha
            .Select(t => new
            {
                id = t.TaskId,
                title = t.Title,
                description = t.Description,
                dueDate = t.DueDate,
                isCompleted = t.IsCompleted,
                // Calculamos si está vencida (solo si no está completa)
                isOverdue = !t.IsCompleted && t.DueDate < DateTime.Today
            })
            .ToListAsync();

        return Json(new { data = tasks });
    }

    [HttpGet]
    public async Task<IActionResult> GetTaskForm(int? id)
    {
        if (id.HasValue)
        {
            var task = await context.TodoTask.FindAsync(id);
            if (task == null) return NotFound();
            return PartialView("_TodoTaskForm", task);
        }
        // Nueva tarea: Fecha de hoy por defecto
        return PartialView("_TodoTaskForm", new TodoTask { DueDate = DateTime.Now });
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] TodoTask task)
    {
        if (!ModelState.IsValid) return Json(new { success = false, message = "Datos inválidos" });

        var username = currentUser.Username;
        var user = await context.Login.FirstOrDefaultAsync(u => u.Username == username);
        task.UserID = user.ID;

        if (task.TaskId > 0)
        {
            var existing = await context.TodoTask.FindAsync(task.TaskId);
            if (existing == null) return NotFound();

            existing.Title = task.Title;
            existing.Description = task.Description;
            existing.DueDate = task.DueDate;

            context.TodoTask.Update(existing);
        }
        else
        {
            task.IsCompleted = false; // Siempre nace pendiente
            context.TodoTask.Add(task);
        }

        await context.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var task = await context.TodoTask.FindAsync(id);
        if (task == null) return Json(new { success = false });

        task.IsCompleted = !task.IsCompleted; // Invertir estado
        await context.SaveChangesAsync();

        return Json(new { success = true, newState = task.IsCompleted });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var task = await context.TodoTask.FindAsync(id);
        if (task != null)
        {
            context.TodoTask.Remove(task);
            await context.SaveChangesAsync();
        }
        return Json(new { success = true });
    }
}
