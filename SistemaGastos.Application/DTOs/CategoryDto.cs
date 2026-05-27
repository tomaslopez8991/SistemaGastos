namespace SistemaGastos.Application.DTOs;

public record CategoryDto(int Id, string Name, string Type, string Icon, string Color, string? Description);

public record SaveCategoryDto(int ID, string Name, string Type, string? Description, string Icon, string Color);
