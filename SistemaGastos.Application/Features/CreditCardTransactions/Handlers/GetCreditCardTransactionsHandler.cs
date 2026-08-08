using MediatR;
using AutoMapper;
using AutoMapper.QueryableExtensions; // Importante para ProjectTo
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Transactions.Queries;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.Transactions.Handlers;

public class GetCreditCardTransactionsHandler(IApplicationDbContext context, ICurrentUserService user)
    : IRequestHandler<GetCreditCardTransactionsQuery, CreditCardTransactionSearchResultDto>
{
    public async Task<CreditCardTransactionSearchResultDto> Handle(GetCreditCardTransactionsQuery request, CancellationToken cancellationToken)
    {
        // Validamos usuario
        if (user.UserId == null) return new CreditCardTransactionSearchResultDto([], 0, new(0, 0, 0, 0, 0, 0));

        // 1. Consulta Base (Filtrada por ID de usuario, más seguro que Username)
        var query = context.CreditCardTransaction
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Include(t => t.SharedWith).ThenInclude(s => s.Person)
            .Where(t => t.Account != null
                     && t.Account.Login != null
                     && t.Account.Login.ID == user.UserId)
            .AsQueryable();

        // 2. Filtro de Búsqueda (Keyword) - Lógica de tu snippet
        if (!string.IsNullOrEmpty(request.Keyword))
        {
            var k = request.Keyword; // Variable local para EF
            query = query.Where(t =>
                t.Description.Contains(k) ||
                t.Category.Name.Contains(k) ||
                t.Account.Currency.Contains(k));
        }

        if (request.CategoryID.HasValue)
            query = query.Where(t => t.CategoryID == request.CategoryID.Value);
        else if (!string.IsNullOrWhiteSpace(request.CategoryName))
            query = query.Where(t => t.Category.Name == request.CategoryName);

        if (request.PersonID.HasValue)
            query = query.Where(t => t.SharedWith.Any(s => s.PersonID == request.PersonID.Value));
        if (request.Installments.HasValue)
            query = query.Where(t => (t.Installments ?? 1) == request.Installments.Value);
        if (request.Fixed.HasValue)
            query = query.Where(t => t.Fixed == request.Fixed.Value);
        if (request.DateFrom.HasValue)
            query = query.Where(t => t.TransactionDate >= request.DateFrom.Value.Date);
        if (request.DateTo.HasValue)
            query = query.Where(t => t.TransactionDate < request.DateTo.Value.Date.AddDays(1));

        // 3. Contar Total (Para la paginación)
        var total = await query.CountAsync(cancellationToken);

        var totalsByType = await query
            .GroupBy(t => new { t.Fixed, t.Account.Currency })
            .Select(g => new { g.Key.Fixed, g.Key.Currency, Total = g.Sum(t => t.Amount) })
            .ToListAsync(cancellationToken);

        decimal Total(bool fixedValue, string currency) =>
            totalsByType.FirstOrDefault(x => x.Fixed == fixedValue && x.Currency == currency)?.Total ?? 0m;
        var totals = new CreditCardTotalsDto(
            Total(false, "ARS") + Total(true, "ARS"),
            Total(false, "USD") + Total(true, "USD"),
            Total(true, "ARS"),
            Total(true, "USD"),
            Total(false, "ARS"),
            Total(false, "USD"));

        // 4. Obtener Datos Paginados y Proyectar a DTO
        var data = await query
            .OrderByDescending(t => t.TransactionDate)
            .Skip(request.Offset)
            .Take(request.Limit)
            .Select(t => new CreditCardTransactionDto
            {
                ID = t.ID,
                Description = t.Description,
                Amount = t.Amount,
                TransactionDate = t.TransactionDate,
                CategoryName = t.Category.Name,
                AccountName = t.Account.Name,
                Currency = t.Account.Currency,
                ActualInstallment = t.ActualInstallment ?? 0,
                Installments = t.Installments ?? 1,
                Fixed = t.Fixed,
                SharedWith = t.SharedWith.Select(s => new CreditCardTransactionPersonDto(s.PersonID, s.Percentage, s.Person == null ? "" : s.Person.Name)).ToList()
            })
            .ToListAsync(cancellationToken);

        // 5. Respuesta en formato PagedResult (equivale a tu new { results, total })
        return new CreditCardTransactionSearchResultDto(data, total, totals);
    }
}
