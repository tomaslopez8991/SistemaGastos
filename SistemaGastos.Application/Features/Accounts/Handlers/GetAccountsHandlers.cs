using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Accounts.Queries;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Application.ViewModels;

namespace SistemaGastos.Application.Features.Accounts.Handlers;

public class GetAccountsHandlers(IApplicationDbContext context, IMapper mapper, ICurrentUserService user)
    : IRequestHandler<GetAccountsQuery, List<AccountDto>>,
      IRequestHandler<GetAccountByIdQuery, AccountDto?>
{
    // 1. Obtener TODAS las cuentas del usuario (para la grilla)
    public async Task<List<AccountDto>> Handle(GetAccountsQuery request, CancellationToken cancellationToken)
    {
        if (user.UserId == null) return [];

        return await context.Account
            .Where(a => a.Login.ID == user.UserId) // Filtro de seguridad
            .OrderBy(a => a.Name)
            .ProjectTo<AccountDto>(mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }

    // 2. Obtener UNA cuenta por ID (para editar)
    public async Task<AccountDto?> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
    {
        if (user.UserId == null) return null;

        return await context.Account
            .Where(a => a.ID == request.Id && a.Login.ID == user.UserId)
            .ProjectTo<AccountDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public class GetAccountTotalsHandler(IApplicationDbContext context, ICurrentUserService user)
    : IRequestHandler<GetAccountTotalsQuery, List<AccountTotalDto>>
    {
        public async Task<List<AccountTotalDto>> Handle(GetAccountTotalsQuery request, CancellationToken cancellationToken)
        {
            if (user.UserId == null) return [];

            return await context.Account
                .Where(a => a.Login.ID == user.UserId)
                .GroupBy(a => a.Currency)
                .Select(g => new AccountTotalDto(g.Key, g.Sum(x => x.Balance)))
                .ToListAsync(cancellationToken);
        }
    }
}