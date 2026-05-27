using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.FixedExpense.Queries;

public record GetAllFixedExpensesQuery(int UserID) : IRequest<List<FixedExpenseDto>>;