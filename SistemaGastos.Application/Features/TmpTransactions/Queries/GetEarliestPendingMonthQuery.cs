using MediatR;

namespace SistemaGastos.Application.Features.TmpTransactions.Queries;

/// <summary>Devuelve "YYYY-MM" del mes más antiguo con TmpTransactions pendientes previos al mes actual, o null si no hay ninguno.</summary>
public record GetEarliestPendingMonthQuery(int UserID) : IRequest<string?>;
