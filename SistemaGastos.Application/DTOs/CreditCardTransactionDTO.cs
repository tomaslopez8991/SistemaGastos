using System.ComponentModel;

namespace SistemaGastos.Application.DTOs;

public record CreditCardTransactionDto
{
    public int ID { get; init; }
    public string Description { get; init; } = string.Empty; // Inicializamos para evitar nulos
    public decimal Amount { get; init; }
    public DateTime TransactionDate { get; init; }
    public int CategoryID { get; init; }
    public int AccountID { get; init; }
    public int? ActualInstallment { get; init; }
    public int? Installments { get; init; }
    public bool Fixed { get; init; }

    // Propiedades de lectura (aplanadas)
    public string CategoryName { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
}

public record CreditCardAccountLookupDto(int ID, string Name, string Currency);

public record CreditCardFormCategoryDto(int ID, string Name);

public record CreditCardFormDto(
    List<CreditCardAccountLookupDto> CreditCards,
    List<CreditCardAccountLookupDto> Accounts,
    List<CreditCardFormCategoryDto> Categories,
    SistemaGastos.Domain.Models.CreditCardTransaction? Transaction);

public record CreditCardTotalsDto(
    decimal TotalArs, decimal TotalUsd,
    decimal FixedArs, decimal FixedUsd,
    decimal VariableArs, decimal VariableUsd);

public record UpdateCreditCardTransactionDto(
    int ID, // El JS envía "ID" en mayúsculas a veces, el binding de .NET es case-insensitive
    string Description,
    decimal Amount,
    DateTime TransactionDate,
    int CategoryID,
    int AccountID,
    int? Installments,
    int? ActualInstallment,
    bool Fixed
);