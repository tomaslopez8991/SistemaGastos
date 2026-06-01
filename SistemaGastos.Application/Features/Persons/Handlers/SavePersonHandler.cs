using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.Persons.Commands;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.Persons.Handlers;

public class SavePersonHandler(IApplicationDbContext context)
    : IRequestHandler<SavePersonCommand, int>
{
    public async Task<int> Handle(SavePersonCommand request, CancellationToken cancellationToken)
    {
        if (request.ID > 0)
        {
            var existing = await context.Person
                .FirstOrDefaultAsync(p => p.ID == request.ID && p.UserID == request.UserID, cancellationToken);
            if (existing == null) return 0;

            existing.Name = request.Name.Trim();
            await context.SaveChangesAsync(cancellationToken);
            return existing.ID;
        }

        var person = new Person
        {
            UserID = request.UserID,
            Name = request.Name.Trim(),
            Active = true
        };
        context.Person.Add(person);
        await context.SaveChangesAsync(cancellationToken);
        return person.ID;
    }
}
