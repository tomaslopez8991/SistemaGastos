using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Accounts.Commands;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.Accounts.Handlers;

public class CreateAccountHandler(IApplicationDbContext context, IMapper mapper, ICurrentUserService user)
    : IRequestHandler<CreateAccountCommand, int>
{
    public async Task<int> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        if (user.UserId == null) throw new UnauthorizedAccessException("Usuario no identificado");

        var entity = mapper.Map<Account>(request.Dto);

        var currentUser = await context.Login.FindAsync([user.UserId], cancellationToken);
        if (currentUser == null) throw new Exception("Usuario no encontrado en BD");

        entity.Login = currentUser;

        context.Account.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}