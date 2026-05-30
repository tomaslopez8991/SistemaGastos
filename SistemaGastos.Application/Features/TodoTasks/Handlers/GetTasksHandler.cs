using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.TodoTasks.Queries;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.TodoTasks.Handlers;

public class GetTasksHandler(IApplicationDbContext context)
    : IRequestHandler<GetTasksQuery, List<TodoTaskDto>>
{
    public async Task<List<TodoTaskDto>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.Today;

        var tasks = await context.TodoTask
            .AsNoTracking()
            .Where(t => t.UserID == request.UserId)
            .OrderBy(t => t.IsCompleted)
            .ThenBy(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .ToListAsync(cancellationToken);

        return tasks.Select(t => new TodoTaskDto(
            t.TaskId,
            t.Title,
            t.Description,
            t.DueDate,
            t.IsCompleted,
            !t.IsCompleted && t.DueDate < today,
            t.Priority,
            t.ReminderDate
        )).ToList();
    }
}
