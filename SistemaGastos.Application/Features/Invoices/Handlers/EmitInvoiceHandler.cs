using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Invoices.Commands;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.Invoices.Handlers;

public class EmitInvoiceHandler(IApplicationDbContext context, IARCAService arcaService)
    : IRequestHandler<EmitInvoiceCommand, EmitInvoiceResultDto>
{
    public async Task<EmitInvoiceResultDto> Handle(
        EmitInvoiceCommand request,
        CancellationToken cancellationToken)
    {
        var transaction = await context.Transaction
            .AsNoTracking()
            .Include(t => t.Account)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(
                t => t.ID == request.Dto.TransactionId && t.Account!.UserID == request.UserId,
                cancellationToken);

        if (transaction is null)
            return new EmitInvoiceResultDto(false, null, null, "Transacción no encontrada.");

        if (transaction.Category?.Type != "Ingreso")
            return new EmitInvoiceResultDto(false, null, null, "Solo se pueden facturar transacciones de tipo Ingreso.");

        return await arcaService.EmitirFacturaCAsync(
            request.Dto,
            transaction.Amount,
            transaction.Description ?? "Sin descripción",
            transaction.Date);
    }
}
