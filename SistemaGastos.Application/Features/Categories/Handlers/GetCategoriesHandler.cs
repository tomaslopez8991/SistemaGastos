using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Categories.Queries;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.Categories.Handlers;

public class GetCategoriesHandler(IApplicationDbContext context)
    : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await context.Category
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.ID, c.Name, c.Type, c.Icon, c.Color, c.Description))
            .ToListAsync(cancellationToken);
    }
}
