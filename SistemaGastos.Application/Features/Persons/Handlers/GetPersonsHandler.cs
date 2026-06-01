using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Persons.Queries;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.Persons.Handlers;

public class GetPersonsHandler(IApplicationDbContext context)
    : IRequestHandler<GetPersonsQuery, List<PersonDto>>
{
    public async Task<List<PersonDto>> Handle(GetPersonsQuery request, CancellationToken cancellationToken)
    {
        return await context.Person
            .Where(p => p.UserID == request.UserID)
            .OrderBy(p => p.Name)
            .Select(p => new PersonDto { ID = p.ID, Name = p.Name, Active = p.Active })
            .ToListAsync(cancellationToken);
    }
}
