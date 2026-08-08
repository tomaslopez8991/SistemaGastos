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

        var accountIds = settings.Select(s => s.AccountID).ToList();
        var recentCutoff = DateTime.Today.AddDays(-90);
        var recentLogs = await context.AccountInterestDailyLog
            .Where(l => l.UserID == request.UserID && accountIds.Contains(l.AccountID) && l.Date >= recentCutoff)
            .OrderByDescending(l => l.Date)
            .Select(l => new AccountInterestDailyLogDto(
                l.ID, l.AccountID, l.Date, l.Balance, l.DayCounter, l.DailyInterest, l.CumulativeInterest))
            .ToListAsync(cancellationToken);

        var logsByAccount = recentLogs.ToLookup(l => l.AccountID);
        var settingDtos = settings.Select(s =>
        {
            var accountLogs = logsByAccount[s.AccountID].ToList();
            var last = accountLogs.FirstOrDefault();
            var currentMonthLogs = accountLogs
                .Where(l => l.Date.Year == DateTime.Today.Year && l.Date.Month == DateTime.Today.Month)
                .ToList();
            var cumulativeInterest = currentMonthLogs.FirstOrDefault()?.CumulativeInterest ?? 0m;
            var accruedVat = s.ApplyVat ? decimal.Round(cumulativeInterest * s.VatRate, 2) : 0m;
            var accruedStampTax = s.ApplyStampTax
                ? decimal.Round(currentMonthLogs.Sum(l => Math.Abs(Math.Min(l.Balance, 0m))) * s.StampTaxAnnualRate / 365m, 2)
                : 0m;

            return new AccountInterestSettingDto(
                s.ID,
                s.AccountID,
                s.Account.Name,
                s.Account.Currency,
                s.InterestRate,
                s.ApplyVat,
                s.VatRate,
                s.ApplyStampTax,
                s.StampTaxAnnualRate,
                s.Enabled,
                s.CreatedAt,
                s.Account.Balance,
                cumulativeInterest,
                accruedVat,
                accruedStampTax,
                cumulativeInterest + accruedVat + accruedStampTax,
                last?.Date);
        }).ToList();

        var monthlyCharges = await context.AccountInterestMonthlyCharge
            .Where(c => c.UserID == request.UserID && accountIds.Contains(c.AccountID))
            .OrderByDescending(c => c.Year).ThenByDescending(c => c.Month)
            .Take(60)
            .Select(c => new AccountInterestMonthlyChargeDto(
                c.ID, c.AccountID, c.Year, c.Month, c.TotalInterest, c.TransactionID))
            .ToListAsync(cancellationToken);

        var usedAccountIds = settings.Select(s => s.AccountID).ToHashSet();
        var availableAccounts = await context.Account
            .Where(a => a.UserID == request.UserID && a.Type != AccountType.TarjetaCredito && !usedAccountIds.Contains(a.ID))
            .OrderBy(a => a.Name)
            .Select(a => new { a.ID, a.Name, a.Currency })
            .ToListAsync(cancellationToken);

        return new AccountInterestPageDto(
            settingDtos,
            recentLogs,
            monthlyCharges,
            availableAccounts.Select(a => (a.ID, a.Name, a.Currency)).ToList());
    }
}
