namespace SistemaGastos.Application.DTOs;

public record NotificationDto(
    string Type,       // "task" | "fixedexpense" | "budget" | "balance"
    int? RefId,
    string Title,
    string Subtitle,
    string Icon,
    string Severity,   // "danger" | "warning" | "info"
    string ActionUrl
);
