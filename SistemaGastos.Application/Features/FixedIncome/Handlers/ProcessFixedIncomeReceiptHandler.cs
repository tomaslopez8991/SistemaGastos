using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.FixedIncome.Commands;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Application.Helpers;
using SistemaGastos.Domain.Enums;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.FixedIncome.Handlers;

public class ProcessFixedIncomeReceiptHandler(IApplicationDbContext context, IDolarService dolarService)
    : IRequestHandler<ProcessFixedIncomeReceiptCommand, ReceiptResultDto>
{
    public async Task<ReceiptResultDto> Handle(ProcessFixedIncomeReceiptCommand request, CancellationToken cancellationToken)
    {
        var income = await context.FixedIncome
            .Include(x => x.Account)
            .Include(x => x.Category)
            .Include(x => x.Person)
            .FirstOrDefaultAsync(x => x.ID == request.FixedIncomeID && x.UserID == request.UserID, cancellationToken);

        if (income is null)
            throw new InvalidOperationException($"Ingreso fijo {request.FixedIncomeID} no encontrado.");

        if (income.PersonID.HasValue)
        {
            if (!request.AccountID.HasValue)
                throw new ArgumentException("Debe seleccionar la cuenta donde acreditar el cobro.");

            var selectedAccount = await context.Account
                .FirstOrDefaultAsync(a => a.ID == request.AccountID.Value
                                       && a.UserID == request.UserID
                                       && a.Type != AccountType.TarjetaCredito,
                    cancellationToken)
                ?? throw new InvalidOperationException("La cuenta seleccionada no es válida.");
            income.Account = selectedAccount;
            income.AccountID = selectedAccount.ID;
        }

        var receiptDate = ResolveReceiptDate(income, request.ReceiptDay);

        decimal amountArs;
        if (request.AmountOverride.HasValue)
        {
            amountArs = request.AmountOverride.Value;
        }
        else
        {
            amountArs = income.Amount;
            if (income.Currency == "USD")
            {
                decimal rate = await dolarService.GetDolarBolsaAsync();
                amountArs = income.Amount * rate;
            }
        }

        // Crear transacción de ingreso y sumar al saldo
        var transaction = new Transaction
        {
            Description = income.PersonID.HasValue
                ? $"Cobro: {income.Person?.Name ?? income.Name}"
                : $"Cobro: {income.Name}",
            Amount = amountArs,
            Date = receiptDate,
            AccountID = income.AccountID,
            CategoryID = income.CategoryID,
            FixedIncomeID = income.ID,
            PersonID = income.PersonID
        };

        income.Account.Balance += amountArs;
        if (income.DistributionEndDay.HasValue)
        {
            var progressMonth = $"{receiptDate.Year}-{receiptDate.Month:D2}";
            if (income.ReceiptProgressYearMonth != progressMonth)
            {
                income.ReceivedAmount = 0;
                income.ReceivedDays = null;
                income.ReceiptProgressYearMonth = progressMonth;
            }
            income.ReceivedAmount += amountArs;
            var receivedDays = DistributionHelper.ParseExcludedDays(income.ReceivedDays);
            if (request.ReceiptDay.HasValue && !receivedDays.Contains(request.ReceiptDay.Value))
                receivedDays.Add(request.ReceiptDay.Value);
            income.ReceivedDays = DistributionHelper.SerializeExcludedDays(receivedDays);

            var totalArs = income.Currency == "USD"
                ? income.Amount * await dolarService.GetDolarBolsaAsync()
                : income.Amount;
            if (income.ReceivedAmount >= totalArs)
                income.LastGeneratedDate = receiptDate;
        }
        else
        {
            income.LastGeneratedDate = receiptDate;
        }

        if (income.PersonID.HasValue && income.Person != null)
        {
            var monthKey = income.CollectionYearMonth
                ?? $"{receiptDate.Year}-{receiptDate.Month:D2}";
            var collectedMonths = string.IsNullOrWhiteSpace(income.Person.CollectedMonths)
                ? new List<string>()
                : income.Person.CollectedMonths.Split(',').Select(x => x.Trim()).ToList();
            if (!collectedMonths.Contains(monthKey))
            {
                collectedMonths.Add(monthKey);
                income.Person.CollectedMonths = string.Join(',', collectedMonths);
            }
        }

        await context.Transaction.AddAsync(transaction, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return new ReceiptResultDto
        {
            TransactionID = transaction.ID,
            Amount = amountArs,
            AccountName = income.Account.Name,
            ReceiptDate = receiptDate,
            Message = "Ingreso registrado y acreditado en la cuenta"
        };
    }

    private static DateTime ResolveReceiptDate(SistemaGastos.Domain.Models.FixedIncome income, int? requestedDay)
    {
        if (income.PersonID.HasValue
            && !string.IsNullOrWhiteSpace(income.CollectionYearMonth)
            && DateTime.TryParseExact(
                $"{income.CollectionYearMonth}-01",
                "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var collectionMonth))
        {
            var day = Math.Clamp(
                requestedDay ?? income.ReceiptDay,
                1,
                DateTime.DaysInMonth(collectionMonth.Year, collectionMonth.Month));
            return new DateTime(collectionMonth.Year, collectionMonth.Month, day)
                .Add(DateTime.Now.TimeOfDay);
        }

        if (requestedDay.HasValue)
        {
            var now = DateTime.Now;
            return new DateTime(now.Year, now.Month, Math.Clamp(requestedDay.Value, 1, DateTime.DaysInMonth(now.Year, now.Month)))
                .Add(now.TimeOfDay);
        }
        return DateTime.Now;
    }
}
