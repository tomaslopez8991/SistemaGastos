using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.TodoTasks.Commands;

public record SaveTodoTaskCommand(SaveTodoTaskDto Dto) : IRequest<bool>;
public record ToggleTodoTaskCommand(int Id, int UserId) : IRequest<(bool Success, bool NewState)>;
public record DeleteTodoTaskCommand(int Id, int UserId) : IRequest<bool>;
