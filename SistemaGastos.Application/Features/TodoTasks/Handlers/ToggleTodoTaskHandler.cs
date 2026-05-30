using MediatR;
using SistemaGastos.Application.Features.TodoTasks.Commands;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.TodoTasks.Handlers;

public class ToggleTodoTaskHandler(IApplicationDbContext context)
    : IRequestHandler<ToggleTodoTaskCommand, (bool Success, bool NewState)>
{
    public async Task<(bool Success, bool NewState)> Handle(ToggleTodoTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await context.TodoTask.FindAsync([request.Id], cancellationToken);
        if (task == null || task.UserID != request.UserId) return (false, false);

        task.IsCompleted = !task.IsCompleted;
        await context.SaveChangesAsync(cancellationToken);

        return (true, task.IsCompleted);
    }
}
