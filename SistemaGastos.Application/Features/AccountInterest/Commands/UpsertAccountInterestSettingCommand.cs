using MediatR;

namespace SistemaGastos.Application.Features.AccountInterest.Commands;

public record UpsertAccountInterestSettingCommand(
    int? SettingID,
    int AccountID,
    decimal InterestRate,
    bool ApplyVat,
    decimal VatRate,
    bool ApplyStampTax,
    decimal StampTaxAnnualRate,
    bool Enabled,
    int UserID
) : IRequest<bool>;

public record ToggleAccountInterestSettingCommand(int SettingID, int UserID) : IRequest<bool>;

public record DeleteAccountInterestSettingCommand(int SettingID, int UserID) : IRequest<bool>;
