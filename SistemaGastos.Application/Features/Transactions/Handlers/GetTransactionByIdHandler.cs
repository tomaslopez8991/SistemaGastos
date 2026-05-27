using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Transactions.Queries;
using SistemaGastos.Application.Interfaces;
using System.Globalization;

namespace SistemaGastos.Application.Features.Transactions.Handlers;

public class GetTransactionByIdHandler(IApplicationDbContext context)
    : IRequestHandler<GetTransactionByIdQuery, TransactionDTO>
{
    public async Task<TransactionDTO> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        var transaction = await context.Transaction
            .AsNoTracking()
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.FixedExpense)
            .FirstOrDefaultAsync(t => t.ID == request.ID && t.Account.UserID == request.UserID, cancellationToken);

        if (transaction == null)
            throw new KeyNotFoundException($"Transacción con ID {request.ID} no encontrada");

        var culture = new CultureInfo("es-AR");

        // ✅ Derivar el tipo desde Category.Type
        var isIncome = transaction.Category.Type == "Ingreso";

        return new TransactionDTO
        {
            ID = transaction.ID,
            Date = transaction.Date,
            Amount = transaction.Amount,

            // ✅ Propiedades derivadas de Category
            CategoryType = transaction.Category.Type,
            IsIncome = isIncome,

            AccountID = transaction.AccountID,
            AccountName = transaction.Account.Name,
            AccountCurrency = transaction.Account.Currency,
            CategoryID = transaction.CategoryID,
            CategoryName = transaction.Category.Name,
            Description = transaction.Description,
            FixedExpenseID = transaction.FixedExpenseID,
            FixedExpenseName = transaction.FixedExpense?.Name,

            AmountFormatted = transaction.Amount.ToString("C", culture),
            DateFormatted = transaction.Date.ToString("dd/MM/yyyy")
        };
    }
}
