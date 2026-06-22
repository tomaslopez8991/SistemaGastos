using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.Persons.Commands;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.Persons.Handlers;

public class RegisterPersonPaymentHandler(IApplicationDbContext context)
    : IRequestHandler<RegisterPersonPaymentCommand, bool>
{
    public async Task<bool> Handle(RegisterPersonPaymentCommand request, CancellationToken cancellationToken)
    {
        var person = await context.Person
            .FirstOrDefaultAsync(p => p.ID == request.PersonID && p.UserID == request.UserID, cancellationToken);

        if (person is null) return false;

        var account = await context.Account
            .FirstOrDefaultAsync(a => a.ID == request.AccountID && a.UserID == request.UserID, cancellationToken);

        if (account is null) return false;

        // Buscar o crear una categoría "Cobros" de tipo Ingreso
        var category = await context.Category
            .FirstOrDefaultAsync(c => c.Type == "Ingreso", cancellationToken);

        if (category is null) return false;

        // Crear transacción de pago recibido, atribuida a la persona
        var payment = new Transaction
        {
            Description = $"Cobro: {person.Name}",
            Amount = request.Amount,
            Date = DateTime.Now,
            AccountID = request.AccountID,
            CategoryID = category.ID,
            PersonID = request.PersonID
        };

        // Acreditar en la cuenta
        account.Balance += request.Amount;

        // Marcar el mes actual como cobrado en la persona
        var currentMonthKey = $"{DateTime.Now.Year}-{DateTime.Now.Month:D2}";
        var existingMonths = string.IsNullOrEmpty(person.CollectedMonths)
            ? new List<string>()
            : person.CollectedMonths.Split(',').Select(s => s.Trim()).ToList();
        if (!existingMonths.Contains(currentMonthKey))
        {
            existingMonths.Add(currentMonthKey);
            person.CollectedMonths = string.Join(',', existingMonths);
        }

        await context.Transaction.AddAsync(payment, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
