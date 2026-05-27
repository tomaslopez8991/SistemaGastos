using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Transactions.Queries;
using SistemaGastos.Application.Interfaces;
using System.Globalization;

namespace SistemaGastos.Application.Features.Transactions.Handlers;

public class GetTransactionListHandler(IApplicationDbContext context) 
    : IRequestHandler<GetTransactionListQuery, TransactionListDto>
{
    public async Task<TransactionListDto> Handle(GetTransactionListQuery request, CancellationToken cancellationToken)
    {
        var query = context.Transaction
            .AsNoTracking()
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.FixedExpense)
            .Where(t => t.Account.UserID == request.UserID);

        if (request.StartDate.HasValue)
            query = query.Where(t => t.Date >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            query = query.Where(t => t.Date <= request.EndDate.Value);

        if (request.AccountID.HasValue)
            query = query.Where(t => t.AccountID == request.AccountID.Value);

        if (request.CategoryID.HasValue)
            query = query.Where(t => t.CategoryID == request.CategoryID.Value);

        var transactions = await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.ID)
            .ToListAsync(cancellationToken);

        var culture = new CultureInfo("es-AR");

        var transactionsDto = transactions.Select(t => new TransactionDTO
        {
            ID = t.ID,
            Date = t.Date,
            Amount = t.Amount,
            CategoryType = t.Category.Type,
            IsIncome = t.Category.Type == "Ingreso",
            AccountID = t.AccountID,
            AccountName = t.Account.Name,
            AccountCurrency = t.Account.Currency,
            CategoryID = t.CategoryID,
            CategoryName = t.Category.Name,
            Description = t.Description,
            FixedExpenseID = t.FixedExpenseID,
            FixedExpenseName = t.FixedExpense?.Name,
            AmountFormatted = t.Amount.ToString("C", culture),
            DateFormatted = t.Date.ToString("dd/MM/yyyy")
        }).ToList();

        // ✅ Calcular totales basándose en Category.Type
        var totalIncome = transactions
            .Where(t => t.Category.Type == "Ingreso")
            .Sum(t => t.Amount);

        var totalExpense = transactions
            .Where(t => t.Category.Type == "Gasto")
            .Sum(t => t.Amount);

        return new TransactionListDto
        {
            Transactions = transactionsDto,
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            Balance = totalIncome - totalExpense,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };
    }
}
