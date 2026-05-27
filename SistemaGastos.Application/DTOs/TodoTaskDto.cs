namespace SistemaGastos.Application.DTOs;

public record TodoTaskDto(int Id, string Title, string Description, DateTime DueDate, bool IsCompleted, bool IsOverdue);

public record SaveTodoTaskDto(int TaskId, string Title, string Description, DateTime DueDate);
