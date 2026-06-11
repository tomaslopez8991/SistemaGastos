using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.TmpTransactions.Commands;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.TmpTransactions.Handlers;

public class SaveDebtPlanSettingsHandler(IApplicationDbContext context)
    : IRequestHandler<SaveDebtPlanSettingsCommand, bool>
{
    public async Task<bool> Handle(SaveDebtPlanSettingsCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Settings;
        var removedIds = dto.RemovedFixedExpenseIds is { Count: > 0 }
            ? string.Join(',', dto.RemovedFixedExpenseIds)
            : null;

        var entity = await context.DebtPlanSettings
            .FirstOrDefaultAsync(s => s.UserID == request.UserID, cancellationToken);

        if (entity == null)
        {
            entity = new DebtPlanSettings
            {
                UserID = request.UserID
            };
            context.DebtPlanSettings.Add(entity);
        }

        entity.GoalType = dto.GoalType;
        entity.GoalValue = dto.GoalValue;
        entity.ExtraMonthlyIncome = dto.ExtraMonthlyIncome;
        entity.ScenariosMode = dto.ScenariosMode;
        entity.ScenarioMin = dto.ScenarioMin;
        entity.ScenarioNormal = dto.ScenarioNormal;
        entity.ScenarioMax = dto.ScenarioMax;
        entity.RemovedFixedExpenseIds = removedIds;
        entity.UpdatedAt = DateTime.Now;

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
