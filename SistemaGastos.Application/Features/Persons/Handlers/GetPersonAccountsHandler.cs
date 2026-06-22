using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Persons.Queries;
using SistemaGastos.Application.Interfaces;
using System.Globalization;

namespace SistemaGastos.Application.Features.Persons.Handlers;

public class GetPersonAccountsHandler(IApplicationDbContext context)
    : IRequestHandler<GetPersonAccountsQuery, List<PersonAccountDto>>
{
    public async Task<List<PersonAccountDto>> Handle(GetPersonAccountsQuery request, CancellationToken cancellationToken)
    {
        var culture = new CultureInfo("es-AR");

        // Mes de referencia: usa el solicitado o el mes actual
        var now = DateTime.Now;
        int refYear  = request.Year  > 0 ? request.Year  : now.Year;
        int refMonth = request.Month > 0 ? request.Month : now.Month;
        var viewingDate = new DateTime(refYear, refMonth, 1);

        var monthKey = $"{refYear}-{refMonth:D2}";

        var persons = await context.Person
            .Where(p => p.UserID == request.UserID && p.Active)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        if (persons.Count == 0) return new List<PersonAccountDto>();

        var personIds = persons.Select(p => p.ID).ToList();

        // Solo transacciones del mes seleccionado — cobros y gastos impactan únicamente en su mes
        var transactions = await context.Transaction
            .Include(t => t.Category)
            .Where(t => t.PersonID != null && personIds.Contains(t.PersonID.Value)
                     && t.Account.UserID == request.UserID
                     && t.Date.Year == refYear && t.Date.Month == refMonth)
            .ToListAsync(cancellationToken);

        // CC sin filtro de fecha: las cuotas se calculan por mes en el loop
        var cardTransactions = await context.CreditCardTransaction
            .Include(t => t.Account)
            .Where(t => t.PersonID != null && personIds.Contains(t.PersonID.Value))
            .ToListAsync(cancellationToken);

        var fixedExpenses = await context.FixedExpense
            .Where(f => f.PersonID != null && personIds.Contains(f.PersonID.Value) && f.Active)
            .ToListAsync(cancellationToken);

        var result = new List<PersonAccountDto>();

        foreach (var person in persons)
        {
            var items = new List<PersonAccountItemDto>();

            foreach (var t in transactions.Where(t => t.PersonID == person.ID))
            {
                var pct = t.PersonPercentage ?? 100m;
                var attributed = t.Amount * pct / 100m;
                bool isPayment = t.Category?.Type == "Ingreso";

                items.Add(new PersonAccountItemDto
                {
                    Description = t.Description,
                    OriginalAmount = t.Amount,
                    // Cobros (Ingreso) restan la deuda; gastos la suman
                    Amount = isPayment ? -attributed : attributed,
                    AmountFmt = (isPayment ? "-" : "") + attributed.ToString("C", culture),
                    Type = isPayment ? "Payment" : "Transaction",
                    TypeLabel = isPayment ? "Cobro recibido" : "Transacción",
                    Percentage = pct,
                    Date = t.Date,
                    DateFmt = t.Date.ToString("dd/MM/yyyy")
                });
            }

            foreach (var cc in cardTransactions.Where(t => t.PersonID == person.ID))
            {
                var pct = cc.PersonPercentage ?? 100m;

                string typeLabel;
                if ((cc.Installments ?? 1) > 1)
                    typeLabel = $"TC ({cc.Installments} cuotas)";
                else if (cc.Fixed)
                    typeLabel = "TC cargo fijo";
                else
                    typeLabel = "Tarjeta de crédito";

                var attributed = cc.Amount * pct / 100m;
                items.Add(new PersonAccountItemDto
                {
                    Description    = cc.Description,
                    OriginalAmount = cc.Amount,
                    Amount         = attributed,
                    AmountFmt      = attributed.ToString("C", culture),
                    Type           = "CreditCard",
                    TypeLabel      = typeLabel,
                    Percentage     = pct,
                    Date           = cc.TransactionDate,
                    DateFmt        = cc.TransactionDate.ToString("dd/MM/yyyy")
                });
            }

            foreach (var fe in fixedExpenses.Where(f => f.PersonID == person.ID))
            {
                var pct = fe.PersonPercentage ?? 100m;
                var attributed = fe.Amount * pct / 100m;
                items.Add(new PersonAccountItemDto
                {
                    Description = fe.Name,
                    OriginalAmount = fe.Amount,
                    Amount = attributed,
                    AmountFmt = attributed.ToString("C", culture),
                    Type = "FixedExpense",
                    TypeLabel = "Gasto fijo",
                    Percentage = pct,
                    Date = DateTime.Now,
                    DateFmt = "Recurrente"
                });
            }

            items = items.OrderByDescending(i => i.Date).ToList();
            decimal total    = items.Sum(i => i.Amount);
            decimal discount = person.DiscountAmount ?? 0m;
            decimal netOwed  = total - discount;
            bool isCollected = !string.IsNullOrEmpty(person.CollectedMonths)
                && person.CollectedMonths.Split(',').Select(s => s.Trim()).Contains(monthKey);

            result.Add(new PersonAccountDto
            {
                PersonID          = person.ID,
                PersonName        = person.Name,
                TotalOwed         = total,
                TotalOwedFmt      = total.ToString("C", culture),
                DiscountAmount    = discount,
                DiscountAmountFmt = discount > 0 ? discount.ToString("C", culture) : string.Empty,
                NetOwed           = netOwed,
                NetOwedFmt        = netOwed.ToString("C", culture),
                CollectionDay        = person.CollectionDay,
                CollectionFrom       = person.CollectionFrom,
                IsCollectedThisMonth = isCollected,
                Items             = items
            });
        }

        return result;
    }
}
