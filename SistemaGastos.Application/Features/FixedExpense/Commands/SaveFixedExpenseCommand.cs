using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.FixedExpense.Commands;

// Comando unificado que decide internamente si crear o actualizar
public record SaveFixedExpenseCommand(FixedExpenseDto Dto, int UserID) : IRequest<int>;