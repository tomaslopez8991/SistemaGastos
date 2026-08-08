namespace SistemaGastos.Application.DTOs;

public record AdminUserDto(
    int Id,
    string Username,
    string Email,
    string Role,
    bool Active,
    bool EmailConfirmed,
    DateTime CreatedAt,
    bool IsDeleted,
    DateTime? DeletedAt);
