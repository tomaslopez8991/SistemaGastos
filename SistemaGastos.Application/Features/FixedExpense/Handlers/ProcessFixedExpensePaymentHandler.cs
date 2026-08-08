using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.FixedExpense.Commands;
using SistemaGastos.Application.Helpers;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Enums;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.FixedExpense.Handlers;

public class ProcessFixedExpensePaymentHandler(
    IApplicationDbContext context,
    IAccountInterestService accountInterestService)
    : IRequestHandler<ProcessFixedExpensePaymentCommand, PaymentResultDto>
{
    public async Task<PaymentResultDto> Handle(ProcessFixedExpensePaymentCommand request, CancellationToken cancellationToken)
    {
        var expense = await context.FixedExpense
            .Include(x => x.Account)
            .Include(x => x.Category)
            .Include(x => x.CreditCardAccount)
            .FirstOrDefaultAsync(x => x.ID == request.FixedExpenseID && x.UserID == request.UserID, cancellationToken);

        if (expense == null)
            throw new InvalidOperationException($"Fixed expense with ID {request.FixedExpenseID} not found");

        var paymentDate = DateTime.UtcNow;
        int transactionID;
        string message;

        // ✅ LÓGICA: Pago de resumen de TC (auto-generado: debita banco + acredita TC)
        if (expense.CreditCardAccountID.HasValue && expense.CreditCardAccount != null)
        {
            var transaction = new Transaction
            {
                Description = $"Pago TC: {expense.CreditCardAccount.Name}",
                Amount = expense.Amount,
                Date = paymentDate,
                AccountID = expense.AccountID,
                CategoryID = expense.CategoryID,
                FixedExpenseID = expense.ID
            };

            expense.Account.Balance -= expense.Amount;
            expense.CreditCardAccount.Balance += expense.Amount;

            await context.Transaction.AddAsync(transaction, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            transactionID = transaction.ID;
            message = $"Pago de TC registrado y descontado de {expense.Account.Name}";
        }
        // LÓGICA: Decisión según tipo de cuenta del gasto fijo
        else if (expense.Account.Type == AccountType.TarjetaCredito)
        {
            // Crear transacción de tarjeta de crédito (1 cuota)
            var creditCardTransaction = new CreditCardTransaction
            {
                Description = $"Pago: {expense.Name}",
                Amount = expense.Amount,
                TransactionDate = paymentDate,
                AccountID = expense.AccountID,
                CategoryID = expense.CategoryID,
                Installments = 1,
                ActualInstallment = 1,
                Fixed = true,
                FixedExpenseID = expense.ID
            };

            await context.CreditCardTransaction.AddAsync(creditCardTransaction, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            transactionID = creditCardTransaction.ID;
            message = "Pago registrado en tarjeta de crédito";
        }
        else
        {
            // Crear transacción estándar y descontar balance
            var transaction = new Transaction
            {
                Description = $"Pago: {expense.Name}",
                Amount = expense.Amount,
                Date = paymentDate,
                AccountID = expense.AccountID,
                CategoryID = expense.CategoryID,
                FixedExpenseID = expense.ID
            };

            // Descontar del saldo de la cuenta
            expense.Account.Balance -= expense.Amount;

            await context.Transaction.AddAsync(transaction, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            transactionID = transaction.ID;
            message = "Pago registrado y descontado del saldo";
        }

        // Actualizar fecha de último pago generado
        expense.LastGeneratedDate = paymentDate;
        await context.SaveChangesAsync(cancellationToken);

        if (InterestExpenseHelper.IsAutomaticInterest(expense))
            await accountInterestService.RunAccrualAsync(cancellationToken);

        return new PaymentResultDto
        {
            TransactionID = transactionID,
            Amount = expense.Amount,
            AccountName = expense.Account.Name,
            PaymentDate = paymentDate,
            Message = message
        };
    }
}
