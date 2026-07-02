using MediatR;

namespace SistemaGastos.Application.Features.AccountInterest.Queries;

public record GetAccountInterestPageQuery(int UserID) : IRequest<AccountInterestPageDto>;

public record AccountInterestSettingDto(
    int ID,
    int AccountID,
    string AccountName,
    string AccountCurrency,
    decimal InterestRate,
    bool Enabled,
    DateTime CreatedAt,
    decimal CumulativeInterest,
    DateTime? LastLogDate
);

public record AccountInterestDailyLogDto(
    int ID,
    DateTime Date,
    decimal Balance,
    int DayCounter,
    decimal DailyInterest,
    decimal CumulativeInterest
);

public record AccountInterestMonthlyChargeDto(
    int ID,
    int Year,
    int Month,
    decimal TotalInterest,
    int? TransactionID
);

public record AccountInterestPageDto(
    List<AccountInterestSettingDto> Settings,
    List<AccountInterestDailyLogDto> RecentLogs,
    List<AccountInterestMonthlyChargeDto> MonthlyCharges,
    List<(int ID, string Name, string Currency)> AvailableAccounts
);
