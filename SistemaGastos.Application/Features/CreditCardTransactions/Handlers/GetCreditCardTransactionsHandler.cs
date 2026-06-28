using MediatR;
using AutoMapper;
using AutoMapper.QueryableExtensions; // Importante para ProjectTo
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Transactions.Queries;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.Transactions.Handlers;

public class GetCreditCardTransactionsHandler(IApplicationDbContext context, ICurrentUserService user)
    : IRequestHandler<GetCreditCardTransactionsQuery, PagedResult<CreditCardTransactionDto>>
{
    public async Task<PagedResult<CreditCardTransactionDto>> Handle(GetCreditCardTransactionsQuery request, CancellationToken cancellationToken)
    {
        // Validamos usuario
        if (user.UserId == null) return new PagedResult<CreditCardTransactionDto>([], 0);

        // 1. Consulta Base (Filtrada por ID de usuario, más seguro que Username)
        var query = context.CreditCardTransaction
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Where(t => t.Account.Login.ID == user.UserId) // Usamos ID del ICurrentUserService
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

        // 3. Contar Total (Para la paginación)
        var total = await query.CountAsync(cancellationToken);

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
                PersonID = t.PersonID,
                PersonPercentage = t.PersonPercentage,
                PersonName = t.Person?.Name
            })
            .ToListAsync(cancellationToken);

        // 5. Respuesta en formato PagedResult (equivale a tu new { results, total })
        return new PagedResult<CreditCardTransactionDto>(data, total);
    }
}