using MediatR;
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

        if (dto.ID > 0)
        {
            var existing = await context.Category.FindAsync([dto.ID], cancellationToken)
                ?? throw new KeyNotFoundException("Categoría no encontrada");

            existing.Name = dto.Name;
            existing.Type = dto.Type;
            existing.Description = dto.Description;
            existing.Icon = dto.Icon;
            existing.Color = dto.Color;

            context.Category.Update(existing);
        }
        else
        {
            var entity = new Category
            {
                Name = dto.Name,
                Type = dto.Type,
                Description = dto.Description,
                Icon = dto.Icon,
                Color = dto.Color
            };
            context.Category.Add(entity);
        }

        await context.SaveChangesAsync(cancellationToken);
        return dto.ID;
    }
}
