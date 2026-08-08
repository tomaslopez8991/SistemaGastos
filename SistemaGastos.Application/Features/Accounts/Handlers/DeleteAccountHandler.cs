using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.Accounts.Commands;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.Accounts.Handlers;

public class DeleteAccountHandler(IApplicationDbContext context, ICurrentUserService user)
    : IRequestHandler<DeleteAccountCommand, bool>
{
    public async Task<bool> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        if (user.UserId == null) return false;

        var entity = await context.Account
            .FirstOrDefaultAsync(a => a.ID == request.Id && a.UserID == user.UserId, cancellationToken);

        if (entity == null) return false;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var fixedExpenses = await context.FixedExpense
                .Where(f => f.AccountID == entity.ID || f.CreditCardAccountID == entity.ID)
                .ToListAsync(cancellationToken);
            var fixedExpenseIds = fixedExpenses.Select(f => f.ID).ToList();
            var fixedIncomes = await context.FixedIncome
                .Where(f => f.AccountID == entity.ID)
                .ToListAsync(cancellationToken);
            var fixedIncomeIds = fixedIncomes.Select(f => f.ID).ToList();
            var cardTransactions = await context.CreditCardTransaction
                .Where(t => t.AccountID == entity.ID)
                .ToListAsync(cancellationToken);
            var cardTransactionIds = cardTransactions.Select(t => t.ID).ToList();

            var linkedTransactions = await context.Transaction
                .Where(t => (t.FixedExpenseID != null && fixedExpenseIds.Contains(t.FixedExpenseID.Value))
                         || (t.FixedIncomeID != null && fixedIncomeIds.Contains(t.FixedIncomeID.Value)))
                .ToListAsync(cancellationToken);
            foreach (var linked in linkedTransactions)
            {
                if (linked.FixedExpenseID.HasValue && fixedExpenseIds.Contains(linked.FixedExpenseID.Value))
                    linked.FixedExpenseID = null;
                if (linked.FixedIncomeID.HasValue && fixedIncomeIds.Contains(linked.FixedIncomeID.Value))
                    linked.FixedIncomeID = null;
            }

            var fixedExpenseHistory = await context.FixedExpenseHistory
                .Where(h => fixedExpenseIds.Contains(h.FixedExpenseID))
                .ToListAsync(cancellationToken);
            context.FixedExpenseHistory.RemoveRange(fixedExpenseHistory);
            context.FixedExpense.RemoveRange(fixedExpenses);
            context.FixedIncome.RemoveRange(fixedIncomes);

            var cardCollections = await context.CreditCardTransactionCobro
                .Where(c => cardTransactionIds.Contains(c.CreditCardTransactionID))
                .ToListAsync(cancellationToken);
            var cardShares = await context.CreditCardTransactionPerson
                .Where(p => cardTransactionIds.Contains(p.CreditCardTransactionID))
                .ToListAsync(cancellationToken);
            context.CreditCardTransactionCobro.RemoveRange(cardCollections);
            context.CreditCardTransactionPerson.RemoveRange(cardShares);
            context.CreditCardTransaction.RemoveRange(cardTransactions);

            var interestCharges = await context.AccountInterestMonthlyCharge
                .Where(c => c.AccountID == entity.ID)
                .ToListAsync(cancellationToken);
            var interestLogs = await context.AccountInterestDailyLog
                .Where(l => l.AccountID == entity.ID)
                .ToListAsync(cancellationToken);
            var interestSettings = await context.AccountInterestSetting
                .Where(s => s.AccountID == entity.ID)
                .ToListAsync(cancellationToken);
            var cardScenarios = await context.CreditCardProjectionScenario
                .Where(s => s.AccountID == entity.ID)
                .ToListAsync(cancellationToken);
            var projections = await context.TmpTransaction
                .Where(t => t.AccountID == entity.ID)
                .ToListAsync(cancellationToken);
            var transactions = await context.Transaction
                .Where(t => t.AccountID == entity.ID)
                .ToListAsync(cancellationToken);

            context.AccountInterestMonthlyCharge.RemoveRange(interestCharges);
            context.AccountInterestDailyLog.RemoveRange(interestLogs);
            context.AccountInterestSetting.RemoveRange(interestSettings);
            context.CreditCardProjectionScenario.RemoveRange(cardScenarios);
            context.TmpTransaction.RemoveRange(projections);
            context.Transaction.RemoveRange(transactions);

            context.Account.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
