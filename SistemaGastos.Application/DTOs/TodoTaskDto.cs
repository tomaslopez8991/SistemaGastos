namespace SistemaGastos.Application.DTOs;

public record TodoTaskDto(int Id, string Title, string? Description, DateTime DueDate, bool IsCompleted, bool IsOverdue, int Priority, DateTime? ReminderDate);

public record SaveTodoTaskDto(int TaskId, string Title, string? Description, DateTime DueDate, int Priority, DateTime? ReminderDate);

public record TaskNotificationDto(int Id, string Title, DateTime DueDate, bool IsOverdue, bool IsReminderDue);
