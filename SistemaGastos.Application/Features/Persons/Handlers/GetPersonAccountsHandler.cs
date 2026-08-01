using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Persons.Queries;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Application.Helpers;
using System.Globalization;

namespace SistemaGastos.Application.Features.Persons.Handlers;

public class GetPersonAccountsHandler(IApplicationDbContext context, IDolarService dolarService)
    : IRequestHandler<GetPersonAccountsQuery, List<PersonAccountDto>>
{
    public async Task<List<PersonAccountDto>> Handle(GetPersonAccountsQuery request, CancellationToken cancellationToken)
    {
        var culture = new CultureInfo("es-AR");
        var dolarTask = dolarService.GetDolarBolsaAsync();

        // Mes de referencia: usa el solicitado o el mes actual
        var now = DateTime.Now;
        int refYear  = request.Year  > 0 ? request.Year  : now.Year;
        int refMonth = request.Month > 0 ? request.Month : now.Month;
        var viewingDate = new DateTime(refYear, refMonth, 1);

        var monthKey = $"{refYear}-{refMonth:D2}";
        var monthEnd = viewingDate.AddMonths(1);

        var persons = await context.Person
            .Where(p => p.UserID == request.UserID && p.Active)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        if (persons.Count == 0) return new List<PersonAccountDto>();

        var personIds = persons.Select(p => p.ID).ToList();

        // EF Core DbContext no es thread-safe: queries secuenciales
        var personTransactions = await context.Transaction
            .Include(t => t.Category)
            .Where(t => t.PersonID != null && personIds.Contains(t.PersonID.Value)
                     && t.Account.UserID == request.UserID
                     && t.Date < monthEnd)
            .ToListAsync(cancellationToken);
        var transactions = personTransactions
            .Where(t => t.Date.Year == refYear && t.Date.Month == refMonth)
            .ToList();

        var cardTransactions = await context.CreditCardTransaction
            .Include(t => t.Account)
            .Include(t => t.SharedWith)
            .Where(t => t.SharedWith.Any(s => personIds.Contains(s.PersonID)))
            .ToListAsync(cancellationToken);

        var cobros = await context.CreditCardTransactionCobro
            .Where(c => personIds.Contains(c.PersonID))
            .Select(c => new { c.PersonID, c.CreditCardTransactionID, c.CreatedAt })
            .ToListAsync(cancellationToken);

        var fixedExpenses = await context.FixedExpense
            .Where(f => f.PersonID != null && personIds.Contains(f.PersonID.Value) && f.Active)
            .ToListAsync(cancellationToken);
        var dolarRate = await dolarTask;

        var result = new List<PersonAccountDto>();

        foreach (var person in persons)
        {
            var items = new List<PersonAccountItemDto>();
            var monthWasCollected = !string.IsNullOrEmpty(person.CollectedMonths)
                && person.CollectedMonths.Split(',').Select(s => s.Trim()).Contains(monthKey);
            var collectionCutoff = personTransactions
                .Where(t => t.PersonID == person.ID
                         && t.Category?.Type == "Ingreso"
                         && t.Description.StartsWith("Cobro:", StringComparison.OrdinalIgnoreCase)
                         && !cobros.Any(c => c.PersonID == person.ID
                                          && Math.Abs((c.CreatedAt - t.Date).TotalSeconds) <= 5))
                .Select(t => (DateTime?)t.Date)
                .Max();

            foreach (var t in transactions.Where(t => t.PersonID == person.ID
                                                    && t.Category?.Type != "Ingreso"
                                                    && (!collectionCutoff.HasValue || t.Date > collectionCutoff.Value)))
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

            foreach (var cc in cardTransactions.Where(t => t.SharedWith.Any(s => s.PersonID == person.ID)))
            {
                var pct = cc.SharedWith.FirstOrDefault(s => s.PersonID == person.ID)?.Percentage ?? 100m;

                string typeLabel;
                if ((cc.Installments ?? 1) > 1)
                    typeLabel = $"TC ({cc.Installments} cuotas)";
                else if (cc.Fixed)
                    typeLabel = "TC cargo fijo";
                else
                    typeLabel = "Tarjeta de crédito";

                var amountArs = cc.Account?.Currency == "USD" ? cc.Amount * dolarRate : cc.Amount;
                var attributed = amountArs * pct / 100m;
                var collectionDates = cobros
                    .Where(c => c.PersonID == person.ID && c.CreditCardTransactionID == cc.ID)
                    .Select(c => c.CreatedAt)
                    .ToList();
                if (!PersonCreditCardBalanceHelper.ShouldInclude(
                        cc, viewingDate, collectionCutoff, collectionDates))
                    continue;

                var isCobrado = false;

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
                    DateFmt        = cc.TransactionDate.ToString("dd/MM/yyyy"),
                    TransactionID  = cc.ID,
                    IsCobrado      = isCobrado
                });
            }

            foreach (var fe in fixedExpenses.Where(f => f.PersonID == person.ID
                                                    && (f.PaymentYearMonth == null || f.PaymentYearMonth == monthKey)
                                                    && (f.StartDate == null || new DateTime(f.StartDate.Value.Year, f.StartDate.Value.Month, 1) <= viewingDate)
                                                    && (string.IsNullOrEmpty(f.PausedMonths) || !f.PausedMonths.Split(',').Select(s => s.Trim()).Contains(monthKey))
                                                    && (!collectionCutoff.HasValue
                                                        || (f.StartDate.HasValue && f.StartDate.Value > collectionCutoff.Value))))
            {
                var pct = fe.PersonPercentage ?? 100m;
                var amountArs = fe.Currency == "USD" ? fe.Amount * dolarRate : fe.Amount;
                var attributed = amountArs * pct / 100m;
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

            if (monthWasCollected && !collectionCutoff.HasValue)
                items.Clear();

            items = items.OrderByDescending(i => i.Date).ToList();
            decimal total    = items.Sum(i => i.Amount);
            var hasPriorFullCollection = personTransactions.Any(t =>
                t.PersonID == person.ID
                && t.Date < viewingDate
                && t.Category?.Type == "Ingreso"
                && t.Description.StartsWith("Cobro:", StringComparison.OrdinalIgnoreCase));
            decimal discount = hasPriorFullCollection ? 0m : person.DiscountAmount ?? 0m;
            decimal netOwed  = total - discount;
            bool isCollected = monthWasCollected && items.Count == 0;

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
