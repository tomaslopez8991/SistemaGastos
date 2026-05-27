using MediatR;
using SistemaGastos.Application.Features.Categories.Commands;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.Categories.Handlers;

public class DeleteCategoryHandler(IApplicationDbContext context)
    : IRequestHandler<DeleteCategoryCommand, bool>
{
    public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await context.Category.FindAsync([request.Id], cancellationToken);
        if (category == null) return false;

        context.Category.Remove(category);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
