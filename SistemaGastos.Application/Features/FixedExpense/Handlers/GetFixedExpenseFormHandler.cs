using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.FixedExpense.Queries;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.FixedExpense.Handlers;

public class GetFixedExpenseFormHandler(IApplicationDbContext context, IMapper mapper)
    : IRequestHandler<GetFixedExpenseFormQuery, FixedExpenseFormDto>
{
    public async Task<FixedExpenseFormDto> Handle(GetFixedExpenseFormQuery request, CancellationToken cancellationToken)
    {
        var formDto = new FixedExpenseFormDto
        {
            Accounts = await context.Account
                .AsNoTracking()
                .Where(a => a.UserID == request.UserID)
                .Select(a => new AccountDropdownDto
                {
                    ID = a.ID,
                    Name = a.Name,
                    Type = a.Type.ToString()
                })
                .ToListAsync(cancellationToken),

            Categories = await context.Category
                .AsNoTracking()
                .Select(c => new CategoryDropdownDto
                {
                    ID = c.ID,
                    Name = c.Name
                })
                .ToListAsync(cancellationToken),

            Persons = await context.Person
                .AsNoTracking()
                .Where(p => p.UserID == request.UserID && p.Active)
                .OrderBy(p => p.Name)
                .Select(p => new PersonDropdownDto { ID = p.ID, Name = p.Name })
                .ToListAsync(cancellationToken)
        };

        if (request.ID.HasValue && request.ID.Value > 0)
        {
            // Edición: cargar gasto existente
            var expense = await context.FixedExpense
                .AsNoTracking()
                .Include(x => x.Account)
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.ID == request.ID.Value && x.UserID == request.UserID, cancellationToken);

            if (expense != null)
            {
                formDto.Expense = mapper.Map<FixedExpenseDto>(expense);
            }
        }
        else
        {
            // Nuevo gasto: inicializar DTO vacío
            formDto.Expense = new FixedExpenseDto
            {
                ID = 0,
                Active = true,
                PaymentDay = 1
            };
        }

        return formDto;
    }
}
