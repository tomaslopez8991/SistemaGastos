using MediatR;
using SistemaGastos.Application.Features.Categories.Queries;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.Categories.Handlers;

public class GetCategoryByIdHandler(IApplicationDbContext context)
    : IRequestHandler<GetCategoryByIdQuery, Category?>
{
    public async Task<Category?> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        return await context.Category.FindAsync([request.Id], cancellationToken);
    }
}
