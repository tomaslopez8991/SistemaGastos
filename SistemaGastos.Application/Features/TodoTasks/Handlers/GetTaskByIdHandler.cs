using MediatR;
using SistemaGastos.Application.Features.TodoTasks.Queries;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.TodoTasks.Handlers;

public class GetTaskByIdHandler(IApplicationDbContext context)
    : IRequestHandler<GetTaskByIdQuery, TodoTask?>
{
    public async Task<TodoTask?> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        return await context.TodoTask.FindAsync([request.Id], cancellationToken);
    }
}
