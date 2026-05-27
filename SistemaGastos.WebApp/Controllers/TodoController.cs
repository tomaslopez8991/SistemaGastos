using MediatR;
using Microsoft.AspNetCore.Authorization;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.TodoTasks.Commands;
using SistemaGastos.Application.Features.TodoTasks.Queries;

namespace SistemaGastos.Controllers;

[Authorize]
public class TodoController(IMediator mediator, ICurrentUserService currentUser) : Controller
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
        var userId = currentUser.UserId ?? 0;
        if (userId == 0) return Unauthorized();

        var tasks = await mediator.Send(new GetTasksQuery(userId));
        return Json(new { data = tasks });
    }

    [HttpGet]
    public async Task<IActionResult> GetTaskForm(int? id)
    {
        if (id.HasValue)
        {
            var task = await mediator.Send(new GetTaskByIdQuery(id.Value));
            if (task == null) return NotFound();
            return PartialView("_TodoTaskForm", task);
        }
        return PartialView("_TodoTaskForm", new TodoTask { DueDate = DateTime.Now });
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveTodoTaskDto dto)
    {
        if (!ModelState.IsValid) return Json(new { success = false, message = "Datos inválidos" });

        var result = await mediator.Send(new SaveTodoTaskCommand(dto));
        return Json(new { success = result });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var (success, newState) = await mediator.Send(new ToggleTodoTaskCommand(id));
        return Json(new { success, newState });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await mediator.Send(new DeleteTodoTaskCommand(id));
        return Json(new { success = true });
    }
}
