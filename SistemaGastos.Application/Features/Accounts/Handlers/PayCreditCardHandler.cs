using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Accounts.Commands;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Enums;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.Accounts.Handlers;

public class PayCreditCardHandler(IApplicationDbContext context, IDolarService dolarService)
    : IRequestHandler<PayCreditCardCommand, PaymentResultDto>
{
    public async Task<PaymentResultDto> Handle(PayCreditCardCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("El monto del pago debe ser mayor a cero.");

        if (request.PaymentDate == default)
            throw new ArgumentException("La fecha del pago es obligatoria.");

        if (request.PaymentDate.Date > DateTime.Today)
            throw new ArgumentException("La fecha del pago no puede ser futura.");

        var tcAccount = await context.Account
            .FirstOrDefaultAsync(a => a.ID == request.TcAccountId
                                   && a.UserID == request.UserId
                                   && a.Type == AccountType.TarjetaCredito,
                cancellationToken)
            ?? throw new InvalidOperationException("Cuenta TC no encontrada.");

        var sourceAccount = await context.Account
            .FirstOrDefaultAsync(a => a.ID == request.SourceAccountId
                                   && a.UserID == request.UserId
                                   && a.Type != AccountType.TarjetaCredito,
                cancellationToken)
            ?? throw new InvalidOperationException("Cuenta de origen no encontrada.");

        var defaultCategoryId = await context.Category
            .Where(c => c.Type != "Ingreso"
                     && (c.Name == "Tarjeta de crédito" || c.Name == "Tarjeta de credito"))
            .Select(c => c.ID)
            .FirstOrDefaultAsync(cancellationToken);

        if (defaultCategoryId == 0)
            throw new InvalidOperationException("No existe una categoría de gasto para registrar el pago de la tarjeta.");

        var tcFixedExpense = await FindCreditCardExpenseAsync(request, cancellationToken);
        decimal dolarRate = 1m;
        decimal paymentInExpenseCurrency = request.Amount;
        decimal remainingAmountArs = tcFixedExpense?.Amount ?? request.Amount;

        if (tcFixedExpense?.Currency == "USD")
        {
            dolarRate = await dolarService.GetDolarBolsaAsync();
            if (dolarRate <= 0)
                throw new InvalidOperationException("No se pudo obtener la cotización del dólar MEP para registrar el pago.");

            remainingAmountArs = Math.Round(tcFixedExpense.Amount * dolarRate, 2);
            paymentInExpenseCurrency = request.Amount >= remainingAmountArs - 0.01m
                ? tcFixedExpense.Amount
                : request.Amount / dolarRate;
        }

        if (tcFixedExpense != null && request.Amount > remainingAmountArs + 0.01m)
            throw new ArgumentException($"El pago no puede superar el saldo pendiente de {remainingAmountArs:C}.");

        var isTotalPayment = tcFixedExpense == null || request.Amount >= remainingAmountArs - 0.01m;
        var transaction = new Transaction
        {
            Description = $"Pago {(isTotalPayment ? "total" : "parcial")} TC: {tcAccount.Name}",
            Amount = request.Amount,
            Date = request.PaymentDate.Date,
            AccountID = sourceAccount.ID,
            CategoryID = defaultCategoryId,
            FixedExpenseID = tcFixedExpense?.ID
        };

        sourceAccount.Balance -= request.Amount;
        var paymentInCardCurrency = tcAccount.Currency == "USD"
            ? paymentInExpenseCurrency
            : request.Amount;
        tcAccount.Balance += paymentInCardCurrency;
        await context.Transaction.AddAsync(transaction, cancellationToken);

        string remainingMessage = string.Empty;
        if (tcFixedExpense != null)
        {
            tcFixedExpense.Amount -= paymentInExpenseCurrency;
            if (tcFixedExpense.Amount <= 0)
            {
                tcFixedExpense.Amount = 0;
                tcFixedExpense.LastGeneratedDate = request.PaymentDate.Date;
                remainingMessage = " - saldo del mes cubierto.";
            }
            else
            {
                if (!tcFixedExpense.Name.StartsWith("Saldo restante", StringComparison.OrdinalIgnoreCase))
                    tcFixedExpense.Name = $"Saldo restante - {tcFixedExpense.Name}";

                var paymentMonthKey = $"{request.PaymentDate.Year}-{request.PaymentDate.Month:D2}";
                if (tcFixedExpense.PaymentYearMonth == paymentMonthKey)
                {
                    var daysInMonth = DateTime.DaysInMonth(request.PaymentDate.Year, request.PaymentDate.Month);
                    tcFixedExpense.PaymentDay = Math.Min(request.PaymentDate.Day + 1, daysInMonth);
                }

                var remainingArs = tcFixedExpense.Currency == "USD"
                    ? Math.Round(tcFixedExpense.Amount * dolarRate, 2)
                    : tcFixedExpense.Amount;
                remainingMessage = tcFixedExpense.Currency == "USD"
                    ? $" - remanente: USD {tcFixedExpense.Amount:N2} (aprox. {remainingArs:C})."
                    : $" - remanente: {remainingArs:C}.";
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        return new PaymentResultDto
        {
            TransactionID = transaction.ID,
            Amount = request.Amount,
            AccountName = sourceAccount.Name,
            PaymentDate = transaction.Date,
            Message = $"Pago {(isTotalPayment ? "total" : "parcial")} de ${request.Amount:N2} registrado desde {sourceAccount.Name}{remainingMessage}"
        };
    }

    private async Task<SistemaGastos.Domain.Models.FixedExpense?> FindCreditCardExpenseAsync(
        PayCreditCardCommand request,
        CancellationToken cancellationToken)
    {
        if (request.FixedExpenseId.HasValue)
        {
            var selectedExpense = await context.FixedExpense
                .FirstOrDefaultAsync(f => f.ID == request.FixedExpenseId.Value
                                       && f.UserID == request.UserId
                                       && f.CreditCardAccountID == request.TcAccountId,
                    cancellationToken);

            if (selectedExpense != null)
                return selectedExpense;
        }

        return await context.FixedExpense
            .Where(f => f.UserID == request.UserId
                     && f.CreditCardAccountID == request.TcAccountId
                     && f.Active
                     && f.Amount > 0)
            .OrderByDescending(f => f.PaymentYearMonth)
            .ThenByDescending(f => f.ID)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
