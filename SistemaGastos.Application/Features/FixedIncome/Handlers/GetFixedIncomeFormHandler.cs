using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.FixedIncome.Queries;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.FixedIncome.Handlers;

public class GetFixedIncomeFormHandler(IApplicationDbContext context, IDolarService dolarService)
    : IRequestHandler<GetFixedIncomeFormQuery, FixedIncomeFormDto>
{
    public async Task<FixedIncomeFormDto> Handle(GetFixedIncomeFormQuery request, CancellationToken cancellationToken)
    {
        var dolarRate = await dolarService.GetDolarBolsaAsync();

        var formDto = new FixedIncomeFormDto
        {
            DolarMep = dolarRate,
            Accounts = await context.Account
                .AsNoTracking()
                .Where(a => a.UserID == request.UserID)
                .Select(a => new AccountDropdownDto { ID = a.ID, Name = a.Name, Type = a.Type.ToString() })
                .ToListAsync(cancellationToken),
            Categories = await context.Category
                .AsNoTracking()
                .Where(c => c.Type == "Ingreso")
                .Select(c => new CategoryDropdownDto { ID = c.ID, Name = c.Name })
                .ToListAsync(cancellationToken)
        };

        if (request.ID.HasValue && request.ID.Value > 0)
        {
            var existing = await context.FixedIncome
                .AsNoTracking()
                .Include(x => x.Account)
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.ID == request.ID.Value && x.UserID == request.UserID, cancellationToken);

            if (existing is not null)
            {
                formDto.Income = new FixedIncomeDto
                {
                    ID = existing.ID,
                    Name = existing.Name,
                    Amount = existing.Amount,
                    Currency = existing.Currency,
                    ReceiptDay = existing.ReceiptDay,
                    CategoryID = existing.CategoryID,
                    AccountID = existing.AccountID,
                    LogoUrl = existing.LogoUrl,
                    Active = existing.Active,
                    StartDate = existing.StartDate
                };
            }
        }
        else
        {
            formDto.Income = new FixedIncomeDto { ID = 0, Active = true, ReceiptDay = 1, Currency = "ARS" };
        }

        return formDto;
    }
}
