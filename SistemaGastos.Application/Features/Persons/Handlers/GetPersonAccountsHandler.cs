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
                int totalInstallments = cc.Installments ?? 1;

                decimal effectiveAmount;
                string typeLabel;

                if (totalInstallments > 1)
                {
                    // Usar los meses transcurridos solo para saber en qué cuota estamos
                    // y si el item ya fue consumido. El monto NO cambia.
                    var purchaseDate = new DateTime(cc.TransactionDate.Year, cc.TransactionDate.Month, 1);
                    int elapsedMonths = (viewingDate.Year - purchaseDate.Year) * 12
                                      + (viewingDate.Month - purchaseDate.Month) + 1;

                    // Compra todavía no vence en este mes
                    if (elapsedMonths <= 0) continue;

                    // Todas las cuotas ya vencieron → quitar de la grilla
                    if (elapsedMonths > totalInstallments) continue;

                    int currentInstallment = Math.Min(elapsedMonths, totalInstallments);
                    effectiveAmount = cc.Amount; // monto original sin modificar
                    typeLabel = $"TC ({currentInstallment}/{totalInstallments} cuotas)";
                }
                else
                {
                    // Sin cuotas: solo aparece en el mes de la transacción
                    if (cc.TransactionDate.Year != refYear || cc.TransactionDate.Month != refMonth) continue;
                    effectiveAmount = cc.Amount;
                    typeLabel = "Tarjeta de crédito";
                }

                var attributed = effectiveAmount * pct / 100m;
                items.Add(new PersonAccountItemDto
                {
                    Description = cc.Description,
                    OriginalAmount = cc.Amount,
                    Amount = attributed,
                    AmountFmt = attributed.ToString("C", culture),
                    Type = "CreditCard",
                    TypeLabel = typeLabel,
                    Percentage = pct,
                    Date = cc.TransactionDate,
                    DateFmt = cc.TransactionDate.ToString("dd/MM/yyyy")
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
            decimal total = items.Sum(i => i.Amount);

            result.Add(new PersonAccountDto
            {
                PersonID = person.ID,
                PersonName = person.Name,
                TotalOwed = total,
                TotalOwedFmt = total.ToString("C", culture),
                Items = items
            });
        }

        return result;
    }
}
