using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.Accounts.Commands;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.Accounts.Handlers;

public class UpdateAccountHandler(IApplicationDbContext context, IMapper mapper, ICurrentUserService user)
    : IRequestHandler<UpdateAccountCommand, bool>
{
    public async Task<bool> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        if (user.UserId == null) return false;

        var entity = await context.Account
            .FirstOrDefaultAsync(a => a.ID == request.Dto.ID && a.Login.ID == user.UserId, cancellationToken);

        if (entity == null) return false;

        mapper.Map(request.Dto, entity);

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}