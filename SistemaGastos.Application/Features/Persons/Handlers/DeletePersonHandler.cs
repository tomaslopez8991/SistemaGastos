using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.Persons.Commands;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.Persons.Handlers;

public class DeletePersonHandler(IApplicationDbContext context)
    : IRequestHandler<DeletePersonCommand, bool>
{
    public async Task<bool> Handle(DeletePersonCommand request, CancellationToken cancellationToken)
    {
        var person = await context.Person
            .FirstOrDefaultAsync(p => p.ID == request.ID && p.UserID == request.UserID, cancellationToken);
        if (person == null) return false;

        var transactions = await context.Transaction
            .Where(t => t.PersonID == request.ID)
            .ToListAsync(cancellationToken);
        foreach (var t in transactions) t.PersonID = null;

        var ccAttribusions = await context.CreditCardTransactionPerson
            .Where(s => s.PersonID == request.ID)
            .ToListAsync(cancellationToken);
        context.CreditCardTransactionPerson.RemoveRange(ccAttribusions);

        var fixedExpenses = await context.FixedExpense
            .Where(f => f.PersonID == request.ID)
            .ToListAsync(cancellationToken);
        foreach (var f in fixedExpenses) f.PersonID = null;

        context.Person.Remove(person);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
