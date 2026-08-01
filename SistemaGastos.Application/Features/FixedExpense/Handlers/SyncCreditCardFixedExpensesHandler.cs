using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.FixedExpense.Commands;
using SistemaGastos.Application.Helpers;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Enums;

namespace SistemaGastos.Application.Features.FixedExpense.Handlers;

public class SyncCreditCardFixedExpensesHandler(IApplicationDbContext context, IDolarService dolarService)
    : IRequestHandler<SyncCreditCardFixedExpensesCommand, int>
{
    public async Task<int> Handle(SyncCreditCardFixedExpensesCommand request, CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var closingMonth = new DateTime(request.Year, request.Month, 1);

        if (request.Year != today.Year || request.Month != today.Month)
            return 0;

        var ccAccounts = await context.Account
            .Where(a => a.UserID == request.UserID && a.Type == AccountType.TarjetaCredito)
            .ToListAsync(cancellationToken);

        if (ccAccounts.Count == 0) return 0;

        // A generated statement is a snapshot. A later synchronization must
        // never recalculate it from the card's live balance.
        var existingExpenses = await context.FixedExpense
            .Where(f => f.UserID == request.UserID && f.CreditCardAccountID != null)
            .ToListAsync(cancellationToken);

        var defaultAccountID = await context.Account
            .Where(a => a.UserID == request.UserID && a.Type != AccountType.TarjetaCredito)
            .OrderBy(a => a.ID)
            .Select(a => a.ID)
            .FirstOrDefaultAsync(cancellationToken);

        if (defaultAccountID == 0) return 0;

        var defaultCategoryID = await context.Category
            .Where(c => c.Type != "Ingreso"
                     && (c.Name == "Tarjeta de crédito" || c.Name == "Tarjeta de credito"))
            .Select(c => c.ID)
            .FirstOrDefaultAsync(cancellationToken);

        if (defaultCategoryID == 0) return 0;

        int changes = 0;
        var dueMonthsToSync = new HashSet<DateTime>();

        foreach (var cc in ccAccounts)
        {
            if (!cc.ClosingDay.HasValue || today.Day <= cc.ClosingDay.Value)
                continue;

            var dueMonth = closingMonth.AddMonths(cc.DueMonthOffset ?? 1);
            var dueMonthKey = $"{dueMonth.Year}-{dueMonth.Month:D2}";
            dueMonthsToSync.Add(dueMonth);

            // Older versions could reopen an already-paid statement after the
            // next closing date. LastGeneratedDate proves that it was settled.
            foreach (var paidSnapshot in existingExpenses.Where(f =>
                         f.CreditCardAccountID == cc.ID
                         && f.LastGeneratedDate.HasValue
                         && string.CompareOrdinal(f.PaymentYearMonth, dueMonthKey) < 0
                         && f.Amount > 0))
            {
                paidSnapshot.Amount = 0;
                paidSnapshot.Name = RemoveRemainingPrefix(paidSnapshot.Name);
                changes++;
            }

            // Idempotency: after closing, only payment operations may reduce
            // the amount of this statement.
            if (existingExpenses.Any(f => f.CreditCardAccountID == cc.ID
                                       && f.PaymentYearMonth == dueMonthKey))
                continue;

            var monthlyDue = cc.EffectiveTcProjection;
            if (monthlyDue <= 0) continue;

            var dueDay = cc.DueDay ?? cc.ClosingDay.Value + 10;
            var newExpense = new Domain.Models.FixedExpense
            {
                UserID = request.UserID,
                AccountID = defaultAccountID,
                CategoryID = defaultCategoryID,
                CreditCardAccountID = cc.ID,
                PaymentYearMonth = dueMonthKey,
                Name = $"Total TC - {cc.Name}",
                Amount = monthlyDue,
                Currency = cc.Currency,
                PaymentDay = Math.Min(dueDay, DateTime.DaysInMonth(dueMonth.Year, dueMonth.Month)),
                Active = true,
                StartDate = dueMonth
            };

            await context.FixedExpense.AddAsync(newExpense, cancellationToken);
            changes++;
        }

        // Person collection snapshots are created for the upcoming collection
        // cycle. A card configured with a same-month due date must not recreate
        // historical person income records in the closing month.
        foreach (var dueMonth in dueMonthsToSync.Where(month => month > closingMonth))
            changes += await SyncPersonCollectionsAsync(
                request.UserID, dueMonth, defaultAccountID, cancellationToken);

        if (changes > 0)
            await context.SaveChangesAsync(cancellationToken);

        return changes;
    }

    private static string RemoveRemainingPrefix(string name)
    {
        const string prefix = "Saldo restante - ";
        return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? name[prefix.Length..]
            : name;
    }

    private async Task<int> SyncPersonCollectionsAsync(
        int userID,
        DateTime dueMonth,
        int defaultAccountID,
        CancellationToken cancellationToken)
    {
        var monthKey = $"{dueMonth.Year}-{dueMonth.Month:D2}";
        var monthEnd = dueMonth.AddMonths(1);
        var persons = await context.Person
            .Where(p => p.UserID == userID && p.Active && p.CollectionDay.HasValue)
            .ToListAsync(cancellationToken);
        if (persons.Count == 0) return 0;

        var personIDs = persons.Select(p => p.ID).ToList();
        var existingPersonIDs = await context.FixedIncome
            .Where(f => f.UserID == userID
                     && f.PersonID.HasValue
                     && f.CollectionYearMonth == monthKey)
            .Select(f => f.PersonID!.Value)
            .ToListAsync(cancellationToken);
        var existingSet = existingPersonIDs.ToHashSet();

        var cardTransactions = await context.CreditCardTransaction
            .Include(t => t.Account)
            .Include(t => t.SharedWith)
            .Where(t => t.SharedWith.Any(s => personIDs.Contains(s.PersonID)))
            .ToListAsync(cancellationToken);
        var individualCollections = await context.CreditCardTransactionCobro
            .Where(c => personIDs.Contains(c.PersonID))
            .ToListAsync(cancellationToken);
        var personTransactions = await context.Transaction
            .Include(t => t.Category)
            .Where(t => t.PersonID.HasValue
                     && personIDs.Contains(t.PersonID.Value)
                     && t.Account != null
                     && t.Account.UserID == userID
                     && t.Date < monthEnd)
            .ToListAsync(cancellationToken);
        var personFixedExpenses = await context.FixedExpense
            .Where(f => f.PersonID.HasValue
                     && personIDs.Contains(f.PersonID.Value)
                     && f.UserID == userID
                     && f.Active)
            .ToListAsync(cancellationToken);

        var incomeCategoryID = await context.Category
            .Where(c => c.Type == "Ingreso")
            .OrderBy(c => c.ID)
            .Select(c => c.ID)
            .FirstOrDefaultAsync(cancellationToken);
        if (incomeCategoryID == 0) return 0;

        var dolarRate = await dolarService.GetDolarBolsaAsync();
        int created = 0;

        foreach (var person in persons.Where(p => !existingSet.Contains(p.ID)))
        {
            if (!string.IsNullOrWhiteSpace(person.CollectionFrom)
                && string.CompareOrdinal(person.CollectionFrom, monthKey) > 0)
                continue;

            var fullCollectionCutoff = personTransactions
                .Where(t => t.PersonID == person.ID
                         && t.Category?.Type == "Ingreso"
                         && t.Description.StartsWith("Cobro:", StringComparison.OrdinalIgnoreCase)
                         && !individualCollections.Any(c => c.PersonID == person.ID
                                                         && Math.Abs((c.CreatedAt - t.Date).TotalSeconds) <= 5))
                .Select(t => (DateTime?)t.Date)
                .Max();

            var cardBalance = cardTransactions
                .Where(cc => cc.SharedWith.Any(s => s.PersonID == person.ID)
                          && PersonCreditCardBalanceHelper.ShouldInclude(
                              cc,
                              dueMonth,
                              fullCollectionCutoff,
                              individualCollections
                                  .Where(c => c.PersonID == person.ID
                                           && c.CreditCardTransactionID == cc.ID)
                                  .Select(c => c.CreatedAt)))
                .Sum(cc =>
                {
                    var amountArs = cc.Account?.Currency == "USD" ? cc.Amount * dolarRate : cc.Amount;
                    var percentage = cc.SharedWith.First(s => s.PersonID == person.ID).Percentage;
                    return amountArs * percentage / 100m;
                });

            var fixedBalance = personFixedExpenses
                .Where(f => f.PersonID == person.ID
                         && (f.PaymentYearMonth == null || f.PaymentYearMonth == monthKey)
                         && (f.StartDate == null || new DateTime(f.StartDate.Value.Year, f.StartDate.Value.Month, 1) <= dueMonth)
                         && (string.IsNullOrEmpty(f.PausedMonths)
                             || !f.PausedMonths.Split(',').Select(x => x.Trim()).Contains(monthKey)))
                .Sum(f => (f.Currency == "USD" ? f.Amount * dolarRate : f.Amount)
                        * (f.PersonPercentage ?? 100m) / 100m);

            var monthTransactions = personTransactions
                .Where(t => t.PersonID == person.ID
                         && t.Date.Year == dueMonth.Year
                         && t.Date.Month == dueMonth.Month
                         && t.Category?.Type != "Ingreso"
                         && (!fullCollectionCutoff.HasValue || t.Date > fullCollectionCutoff.Value))
                .Sum(t => t.Amount * (t.PersonPercentage ?? 100m) / 100m);

            var hasPriorFullCollection = personTransactions.Any(t =>
                t.PersonID == person.ID
                && t.Date < dueMonth
                && t.Category?.Type == "Ingreso"
                && t.Description.StartsWith("Cobro:", StringComparison.OrdinalIgnoreCase));
            var discount = hasPriorFullCollection ? 0m : person.DiscountAmount ?? 0m;
            var amount = Math.Round(cardBalance + fixedBalance + monthTransactions - discount, 2);
            if (amount <= 0) continue;

            await context.FixedIncome.AddAsync(new Domain.Models.FixedIncome
            {
                UserID = userID,
                AccountID = defaultAccountID,
                CategoryID = incomeCategoryID,
                PersonID = person.ID,
                CollectionYearMonth = monthKey,
                Name = $"Cuenta de {person.Name}",
                Amount = amount,
                Currency = "ARS",
                ReceiptDay = Math.Min(person.CollectionDay!.Value, DateTime.DaysInMonth(dueMonth.Year, dueMonth.Month)),
                Active = true,
                StartDate = dueMonth
            }, cancellationToken);
            created++;
        }

        return created;
    }
}
