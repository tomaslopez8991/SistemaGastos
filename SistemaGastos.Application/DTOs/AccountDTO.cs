using SistemaGastos.Domain.Enums;

namespace SistemaGastos.Application.DTOs;

public record AccountDto
{
    public int ID { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public decimal Balance { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Cbu { get; init; } = string.Empty;
    public AccountType Type { get; init; }
    public int? ClosingDay {  get; init; }
    public int? DueDay { get; init; }
    public int? DueMonthOffset { get; init; }
}

public record CreateAccountDto(
    string Name,
    string Currency,
    decimal Balance,
    string? Description,
    string? Cbu,
    AccountType Type,
    int? ClosingDay,
    int? DueDay,
    int? DueMonthOffset
);

public record UpdateAccountDto(
    int ID,
    string Name,
    string Currency,
    decimal Balance,
    string? Description,
    string? Cbu,
    AccountType Type,
    int? ClosingDay,
    int? DueDay,
    int? DueMonthOffset
);

public record AccountTotalDto(string Currency, decimal Total);

public record TransferDto(
    int OriginAccountId,
    int DestinationAccountId,
    decimal Amount,
    DateTime Date
);

public record AccountLookupDto(int Id, string DisplayName);