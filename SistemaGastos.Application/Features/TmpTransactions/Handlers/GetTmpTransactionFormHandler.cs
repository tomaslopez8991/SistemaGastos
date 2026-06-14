using MediatR;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.TmpTransactions.Queries;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.TmpTransactions.Handlers;

public class GetTmpTransactionFormHandler(IApplicationDbContext context, IDolarService dolarService)
    : IRequestHandler<GetTmpTransactionFormQuery, TmpTransactionFormDto>
{
    public async Task<TmpTransactionFormDto> Handle(GetTmpTransactionFormQuery request, CancellationToken cancellationToken)
    {
        var formDto = new TmpTransactionFormDto
        {
            DolarMep = await dolarService.GetDolarBolsaAsync()
        };

        // Categories como SelectListItem
        formDto.Categories = await context.Category
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.ID.ToString(),
                Text = c.Name
            })
            .ToListAsync(cancellationToken);

        // Accounts agrupados por moneda como SelectListItem
        var accountsRaw = await context.Account
            .AsNoTracking()
            .Where(a => a.UserID == request.UserID)
            .OrderBy(a => a.Currency).ThenBy(a => a.Name)
            .ToListAsync(cancellationToken);

        var accountsList = new List<SelectListItem>();
        var currencies = accountsRaw.Select(a => a.Currency).Distinct().ToList();

        foreach (var currency in currencies)
        {
            var group = new SelectListGroup { Name = currency };
            foreach (var acc in accountsRaw.Where(a => a.Currency == currency))
            {
                accountsList.Add(new SelectListItem
                {
                    Value = acc.ID.ToString(),
                    Text = acc.Name,
                    Group = group
                });
            }
        }
        formDto.Accounts = accountsList;

        // Próximos 11 meses
        var nextMonths = new List<MonthSelectionDto>();
        var date = DateTime.Today.AddMonths(1);
        for (int i = 0; i < 11; i++)
        {
            nextMonths.Add(new MonthSelectionDto
            {
                Value = date.ToString("yyyy-MM"),
                Text = date.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-ES")).ToUpper()
            });
            date = date.AddMonths(1);
        }
        formDto.NextMonths = nextMonths;

        // ✅ SIEMPRE inicializar Transaction (nunca dejarla null)
        formDto.Transaction = new TmpTransactionDto
        {
            ID = 0,
            Description = string.Empty,
            Amount = 0,
            CategoryID = 0,
            AccountID = null,
            DateTransaction = DateTime.Now,
            EsRecurrente = false
        };

        // Cargar transacción si se está editando
        if (request.ID.HasValue && request.ID.Value > 0)
        {
            var transaction = await context.TmpTransaction
                .AsNoTracking()
                .Include(t => t.Category)
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.ID == request.ID.Value && t.UserID == request.UserID, cancellationToken);

            if (transaction != null)
            {
                formDto.Transaction = new TmpTransactionDto
                {
                    ID = transaction.ID,
                    Description = transaction.Description,
                    Amount = transaction.Amount,
                    Currency = transaction.Currency,
                    CategoryID = transaction.CategoryID,
                    AccountID = transaction.AccountID,
                    DateTransaction = transaction.DateTransaction,
                    EsRecurrente = (bool)transaction.EsRecurrente,
                    DistributionEndDay = transaction.DistributionEndDay,
                    ExcludedDays = transaction.ExcludedDays
                };
            }
        }

        return formDto;
    }
}