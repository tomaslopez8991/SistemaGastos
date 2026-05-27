using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.TodoTasks.Commands;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.TodoTasks.Handlers;

public class SaveTodoTaskHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<SaveTodoTaskCommand, bool>
{
    public async Task<bool> Handle(SaveTodoTaskCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId == null) throw new UnauthorizedAccessException("Usuario no identificado");

        var dto = request.Dto;
        var user = await context.Login
            .FirstOrDefaultAsync(u => u.Username == currentUser.Username, cancellationToken);
        if (user == null) return false;

        if (dto.TaskId > 0)
        {
            var existing = await context.TodoTask.FindAsync([dto.TaskId], cancellationToken);
            if (existing == null) return false;

            existing.Title = dto.Title;
            existing.Description = dto.Description;
            existing.DueDate = dto.DueDate;

            context.TodoTask.Update(existing);
        }
        else
        {
            var task = new TodoTask
            {
                Title = dto.Title,
                Description = dto.Description,
                DueDate = dto.DueDate,
                IsCompleted = false,
                UserID = user.ID
            };
            context.TodoTask.Add(task);
        }

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
