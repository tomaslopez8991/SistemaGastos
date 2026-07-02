using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.AccountInterest.Queries;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Enums;

namespace SistemaGastos.Application.Features.AccountInterest.Handlers;

public class GetAccountInterestPageHandler(IApplicationDbContext context)
    : IRequestHandler<GetAccountInterestPageQuery, AccountInterestPageDto>
{
    public async Task<AccountInterestPageDto> Handle(GetAccountInterestPageQuery request, CancellationToken cancellationToken)
    {
        var settings = await context.AccountInterestSetting
            .Include(s => s.Account)
            .Where(s => s.UserID == request.UserID)
            .OrderBy(s => s.Account.Name)
            .ToListAsync(cancellationToken);

        var settingIds = settings.Select(s => s.AccountID).ToList();

        var lastLogs = await context.AccountInterestDailyLog
            .Where(l => l.UserID == request.UserID && settingIds.Contains(l.AccountID))
            .GroupBy(l => l.AccountID)
            .Select(g => new { AccountID = g.Key, Last = g.OrderByDescending(l => l.Date).First() })
            .ToListAsync(cancellationToken);

        var lastLogMap = lastLogs.ToDictionary(x => x.AccountID, x => x.Last);

        var settingDtos = settings.Select(s =>
        {
            lastLogMap.TryGetValue(s.AccountID, out var last);
            return new AccountInterestSettingDto(
                s.ID,
                s.AccountID,
                s.Account.Name,
                s.Account.Currency,
                s.InterestRate,
                s.Enabled,
                s.CreatedAt,
                last?.CumulativeInterest ?? 0m,
                last?.Date
            );
        }).ToList();

        // Últimos 30 días de log del primer setting activo
        var primaryAccountId = settings.FirstOrDefault(s => s.Enabled)?.AccountID;
        var recentLogs = primaryAccountId.HasValue
            ? await context.AccountInterestDailyLog
                .Where(l => l.AccountID == primaryAccountId.Value)
                .OrderByDescending(l => l.Date)
                .Take(30)
                .Select(l => new AccountInterestDailyLogDto(l.ID, l.Date, l.Balance, l.DayCounter, l.DailyInterest, l.CumulativeInterest))
                .ToListAsync(cancellationToken)
            : new List<AccountInterestDailyLogDto>();

        var monthlyCharges = await context.AccountInterestMonthlyCharge
            .Where(c => c.UserID == request.UserID)
            .OrderByDescending(c => c.Year).ThenByDescending(c => c.Month)
            .Take(24)
            .Select(c => new AccountInterestMonthlyChargeDto(c.ID, c.Year, c.Month, c.TotalInterest, c.TransactionID))
            .ToListAsync(cancellationToken);

        var usedAccountIds = settings.Select(s => s.AccountID).ToHashSet();
        var availableAccounts = await context.Account
            .Where(a => a.UserID == request.UserID && a.Type != AccountType.TarjetaCredito && !usedAccountIds.Contains(a.ID))
            .Select(a => new { a.ID, a.Name, a.Currency })
            .ToListAsync(cancellationToken);

        return new AccountInterestPageDto(
            settingDtos,
            recentLogs,
            monthlyCharges,
            availableAccounts.Select(a => (a.ID, a.Name, a.Currency)).ToList()
        );
    }
}
