using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Invoices.Queries;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Application.Options;

namespace SistemaGastos.Application.Features.Invoices.Handlers;

public class GetInvoicePreviewHandler(
    IApplicationDbContext context,
    IOptions<FiscalConfigOptions> fiscalOptions)
    : IRequestHandler<GetInvoicePreviewQuery, InvoicePreviewDto?>
{
    public async Task<InvoicePreviewDto?> Handle(
        GetInvoicePreviewQuery request,
        CancellationToken cancellationToken)
    {
        var transaction = await context.Transaction
            .AsNoTracking()
            .Include(t => t.Account)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(
                t => t.ID == request.TransactionId && t.Account!.UserID == request.UserId,
                cancellationToken);

        if (transaction is null) return null;

        var config = fiscalOptions.Value;
        var isConfigured = !string.IsNullOrWhiteSpace(config.Cuit)
                        && !string.IsNullOrWhiteSpace(config.RazonSocial);

        return new InvoicePreviewDto(
            RazonSocial: isConfigured ? config.RazonSocial : "Sin configurar",
            Cuit: isConfigured ? config.Cuit : "00-00000000-0",
            Domicilio: config.Domicilio,
            PuntoVenta: config.PuntoVenta,
            IsConfigured: isConfigured,
            TipoComprobante: "Factura C",
            Fecha: transaction.Date.ToString("dd/MM/yyyy"),
            Descripcion: string.IsNullOrWhiteSpace(transaction.Description)
                ? "Sin descripción"
                : transaction.Description,
            Importe: transaction.Amount,
            TransactionId: transaction.ID
        );
    }
}
