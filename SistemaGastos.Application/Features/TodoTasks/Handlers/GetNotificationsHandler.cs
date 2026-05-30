using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.TodoTasks.Queries;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.TodoTasks.Handlers;

public class GetNotificationsHandler(IApplicationDbContext context)
    : IRequestHandler<GetNotificationsQuery, List<TaskNotificationDto>>
{
    public async Task<List<TaskNotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var tasks = await context.TodoTask
            .AsNoTracking()
            .Where(t => t.UserID == request.UserId && !t.IsCompleted)
            .Where(t => t.DueDate <= tomorrow || (t.ReminderDate != null && t.ReminderDate <= today))
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .ToListAsync(cancellationToken);

        return tasks.Select(t => new TaskNotificationDto(
            t.TaskId,
            t.Title,
            t.DueDate,
            t.DueDate < today,
            t.ReminderDate != null && t.ReminderDate <= today
        )).ToList();
    }
}
