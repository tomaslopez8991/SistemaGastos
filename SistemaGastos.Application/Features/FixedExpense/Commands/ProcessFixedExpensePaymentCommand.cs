using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.FixedExpense.Commands;

public record ProcessFixedExpensePaymentCommand(int FixedExpenseID, int UserID) : IRequest<PaymentResultDto>;