using MediatR;

namespace SistemaGastos.Application.Features.AccountInterest.Queries;

public record GetAccountInterestPageQuery(int UserID) : IRequest<AccountInterestPageDto>;

public record AccountInterestSettingDto(
    int ID,
    int AccountID,
    string AccountName,
    string AccountCurrency,
    decimal InterestRate,
    bool ApplyVat,
    decimal VatRate,
    bool ApplyStampTax,
    decimal StampTaxAnnualRate,
    bool Enabled,
    DateTime CreatedAt,
    decimal CurrentBalance,
    decimal CumulativeInterest,
    decimal AccruedVat,
    decimal AccruedStampTax,
    decimal EstimatedTotal,
    DateTime? LastLogDate
);

public record AccountInterestDailyLogDto(
    int ID,
    int AccountID,
    DateTime Date,
    decimal Balance,
    int DayCounter,
    decimal DailyInterest,
    decimal CumulativeInterest
);

public record AccountInterestMonthlyChargeDto(
    int ID,
    int AccountID,
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
