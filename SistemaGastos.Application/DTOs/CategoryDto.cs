using System.ComponentModel.DataAnnotations;

namespace SistemaGastos.Application.DTOs;

public record CategoryDto(int Id, string Name, string Type, string Icon, string Color, string? Description);

public record SaveCategoryDto(
    int ID,
    [Required, StringLength(60)] string Name,
    [Required] string Type,
    [StringLength(180)] string? Description,
    [Required, StringLength(80)] string Icon,
    [Required, RegularExpression("^#[0-9A-Fa-f]{6}$")] string Color);
