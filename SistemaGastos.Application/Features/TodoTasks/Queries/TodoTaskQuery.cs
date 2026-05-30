using MediatR;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.TodoTasks.Queries;

public record GetTasksQuery(int UserId) : IRequest<List<TodoTaskDto>>;
public record GetTaskByIdQuery(int Id) : IRequest<TodoTask?>;
public record GetNotificationsQuery(int UserId) : IRequest<List<TaskNotificationDto>>;
