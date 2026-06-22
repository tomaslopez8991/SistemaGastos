using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.FixedIncome.Commands;
using SistemaGastos.Application.Interfaces;
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
            .FirstOrDefaultAsync(x => x.ID == request.FixedIncomeID && x.UserID == request.UserID, cancellationToken);

        if (income is null)
            throw new InvalidOperationException($"Ingreso fijo {request.FixedIncomeID} no encontrado.");

        var receiptDate = DateTime.UtcNow;

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
            Description = $"Cobro: {income.Name}",
            Amount = amountArs,
            Date = receiptDate,
            AccountID = income.AccountID,
            CategoryID = income.CategoryID,
            FixedIncomeID = income.ID
        };

        income.Account.Balance += amountArs;
        income.LastGeneratedDate = receiptDate;

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
}
