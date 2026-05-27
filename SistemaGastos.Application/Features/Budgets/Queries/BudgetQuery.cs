using MediatR;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.ViewModels;

namespace SistemaGastos.Application.Features.Budgets.Queries;

public record GetBudgetTabQuery(int UserId, int Month, int Year) : IRequest<List<BudgetStatusViewModel>>;
public record GetBudgetFormQuery(int? Id, int Month, int Year) : IRequest<BudgetFormDto>;
public record GetBudgetDetailsQuery(int BudgetId) : IRequest<BudgetDetailsResult?>;
