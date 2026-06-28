using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.AccountInterest.Commands;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.AccountInterest.Handlers;

public class UpsertAccountInterestSettingHandler(IApplicationDbContext context)
    : IRequestHandler<UpsertAccountInterestSettingCommand, bool>,
      IRequestHandler<ToggleAccountInterestSettingCommand, bool>,
      IRequestHandler<DeleteAccountInterestSettingCommand, bool>
{
    public async Task<bool> Handle(UpsertAccountInterestSettingCommand request, CancellationToken cancellationToken)
    {
        if (request.SettingID.HasValue)
        {
            var existing = await context.AccountInterestSetting
                .FirstOrDefaultAsync(s => s.ID == request.SettingID.Value && s.UserID == request.UserID, cancellationToken)
                ?? throw new Exception("Configuración no encontrada");

            existing.InterestRate = request.InterestRate;
            existing.Enabled = request.Enabled;
        }
        else
        {
            var already = await context.AccountInterestSetting
                .AnyAsync(s => s.AccountID == request.AccountID && s.UserID == request.UserID, cancellationToken);

            if (already) throw new Exception("Esta cuenta ya tiene una configuración de intereses");

            context.AccountInterestSetting.Add(new AccountInterestSetting
            {
                AccountID = request.AccountID,
                UserID = request.UserID,
                InterestRate = request.InterestRate,
                Enabled = request.Enabled,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(ToggleAccountInterestSettingCommand request, CancellationToken cancellationToken)
    {
        var setting = await context.AccountInterestSetting
            .FirstOrDefaultAsync(s => s.ID == request.SettingID && s.UserID == request.UserID, cancellationToken)
            ?? throw new Exception("Configuración no encontrada");

        setting.Enabled = !setting.Enabled;
        await context.SaveChangesAsync(cancellationToken);
        return setting.Enabled;
    }

    public async Task<bool> Handle(DeleteAccountInterestSettingCommand request, CancellationToken cancellationToken)
    {
        var setting = await context.AccountInterestSetting
            .FirstOrDefaultAsync(s => s.ID == request.SettingID && s.UserID == request.UserID, cancellationToken)
            ?? throw new Exception("Configuración no encontrada");

        context.AccountInterestSetting.Remove(setting);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
