using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.Budgets.Commands;

public record SaveBudgetCommand(SaveBudgetDto Dto, int UserId) : IRequest<bool>;
public record DeleteBudgetCommand(int Id) : IRequest<bool>;
public record CloneFromPreviousMonthCommand(int TargetMonth, int TargetYear, int UserId) : IRequest<bool>;
