using MediatR; // Necesario
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.Transactions.Commands;

// IRequest<bool> significa: "Este comando devuelve un booleano al terminar"
// (Podrías devolver un objeto Result más complejo si quisieras)
public record CreateBulkCreditCardTransactionsCommand(List<CreditCardTransactionDto> Transactions) : IRequest<bool>;