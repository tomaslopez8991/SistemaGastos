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
        if (request.InterestRate < 0 || request.VatRate < 0 || request.StampTaxAnnualRate < 0)
            throw new ArgumentException("Las alícuotas no pueden ser negativas.");

        if (request.SettingID.HasValue)
        {
            var existing = await context.AccountInterestSetting
                .FirstOrDefaultAsync(s => s.ID == request.SettingID.Value && s.UserID == request.UserID, cancellationToken)
                ?? throw new InvalidOperationException("Configuración no encontrada");

            existing.InterestRate = request.InterestRate;
            existing.ApplyVat = request.ApplyVat;
            existing.VatRate = request.VatRate;
            existing.ApplyStampTax = request.ApplyStampTax;
            existing.StampTaxAnnualRate = request.StampTaxAnnualRate;
            existing.Enabled = request.Enabled;
        }
        else
        {
            var already = await context.AccountInterestSetting
                .AnyAsync(s => s.AccountID == request.AccountID && s.UserID == request.UserID, cancellationToken);

            if (already) throw new InvalidOperationException("Esta cuenta ya tiene una configuración de intereses");

            context.AccountInterestSetting.Add(new AccountInterestSetting
            {
                AccountID = request.AccountID,
                UserID = request.UserID,
                InterestRate = request.InterestRate,
                ApplyVat = request.ApplyVat,
                VatRate = request.VatRate,
                ApplyStampTax = request.ApplyStampTax,
                StampTaxAnnualRate = request.StampTaxAnnualRate,
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
            ?? throw new InvalidOperationException("Configuración no encontrada");

        setting.Enabled = !setting.Enabled;
        await context.SaveChangesAsync(cancellationToken);
        return setting.Enabled;
    }

    public async Task<bool> Handle(DeleteAccountInterestSettingCommand request, CancellationToken cancellationToken)
    {
        var setting = await context.AccountInterestSetting
            .FirstOrDefaultAsync(s => s.ID == request.SettingID && s.UserID == request.UserID, cancellationToken)
            ?? throw new InvalidOperationException("Configuración no encontrada");

        context.AccountInterestSetting.Remove(setting);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
