using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Accounts.Queries;
using SistemaGastos.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaGastos.Application.Features.Accounts.Handlers;

public class GetTransferTargetsHandler(IApplicationDbContext context, ICurrentUserService user)
    : IRequestHandler<GetTransferTargetsQuery, List<AccountLookupDto>>
{
    public async Task<List<AccountLookupDto>> Handle(GetTransferTargetsQuery request, CancellationToken cancellationToken)
    {
        if (user.UserId == null) return [];

        var origin = await context.Account.FindAsync([request.OriginAccountId], cancellationToken);
        if (origin == null) return [];

        return await context.Account
            .Where(a => a.Login.ID == user.UserId
                        && a.Currency == origin.Currency
                        && a.ID != request.OriginAccountId)
            .Select(a => new AccountLookupDto(a.ID, $"{a.Name} ({a.Currency})"))
            .ToListAsync(cancellationToken);
    }
}