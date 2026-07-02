using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.TmpTransactions.Queries;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.TmpTransactions.Handlers;

public class GetEarliestPendingMonthHandler(IApplicationDbContext context)
    : IRequestHandler<GetEarliestPendingMonthQuery, string?>
{
    public async Task<string?> Handle(GetEarliestPendingMonthQuery request, CancellationToken cancellationToken)
    {
        var startOfCurrentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

        var minDate = await context.TmpTransaction
            .Where(t => t.UserID == request.UserID
                     && t.DateTransaction.HasValue
                     && t.DateTransaction.Value < startOfCurrentMonth)
            .MinAsync(t => (DateTime?)t.DateTransaction, cancellationToken);

        if (minDate == null) return null;

        return $"{minDate.Value.Year}-{minDate.Value.Month:D2}";
    }
}
