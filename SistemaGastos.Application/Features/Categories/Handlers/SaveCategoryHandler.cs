using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.Categories.Commands;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.Categories.Handlers;

public class SaveCategoryHandler(IApplicationDbContext context)
    : IRequestHandler<SaveCategoryCommand, int>
{
    public async Task<int> Handle(SaveCategoryCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var name = dto.Name.Trim();
        var type = string.Equals(dto.Type, "Ingreso", StringComparison.OrdinalIgnoreCase) ? "Ingreso" : "Gasto";
        var description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        var icon = dto.Icon.Trim();
        var color = dto.Color.ToUpperInvariant();

        var normalizedName = name.ToLower();
        var nameExists = await context.Category.AnyAsync(
            category => category.ID != dto.ID && category.Name.ToLower() == normalizedName,
            cancellationToken);
        if (nameExists)
        {
            throw new InvalidOperationException("Ya existe una categoría con ese nombre.");
        }

        Category entity;
        if (dto.ID > 0)
        {
            entity = await context.Category.FindAsync([dto.ID], cancellationToken)
                ?? throw new KeyNotFoundException("Categoría no encontrada.");
            entity.Name = name;
            entity.Type = type;
            entity.Description = description;
            entity.Icon = icon;
            entity.Color = color;
        }
        else
        {
            entity = new Category
            {
                Name = name,
                Type = type,
                Description = description,
                Icon = icon,
                Color = color
            };
            context.Category.Add(entity);
        }

        await context.SaveChangesAsync(cancellationToken);
        return entity.ID;
    }
}
