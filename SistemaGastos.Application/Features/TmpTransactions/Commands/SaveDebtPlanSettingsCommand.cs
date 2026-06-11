using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.TmpTransactions.Commands;

/// <summary>
/// Guarda (o actualiza) la última configuración utilizada por el usuario en el
/// simulador "Plan de Metas Financieras", para auto-guardado/restauración.
/// </summary>
public record SaveDebtPlanSettingsCommand(int UserID, DebtPlanSettingsDto Settings) : IRequest<bool>;
