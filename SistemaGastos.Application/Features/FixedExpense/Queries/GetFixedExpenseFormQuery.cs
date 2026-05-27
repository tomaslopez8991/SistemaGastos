using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.FixedExpense.Queries;

public record GetFixedExpenseFormQuery(int UserID, int? ID = null) : IRequest<FixedExpenseFormDto>;