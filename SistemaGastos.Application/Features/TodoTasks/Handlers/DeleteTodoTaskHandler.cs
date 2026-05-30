using MediatR;
using SistemaGastos.Application.Features.TodoTasks.Commands;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.TodoTasks.Handlers;

public class DeleteTodoTaskHandler(IApplicationDbContext context)
    : IRequestHandler<DeleteTodoTaskCommand, bool>
{
    public async Task<bool> Handle(DeleteTodoTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await context.TodoTask.FindAsync([request.Id], cancellationToken);
        if (task == null) return true;
        if (task.UserID != request.UserId) return false;

        context.TodoTask.Remove(task);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
