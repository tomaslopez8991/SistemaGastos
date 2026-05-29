using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Interfaces;

public interface IARCAService
{
    bool IsConfigured { get; }

    Task<EmitInvoiceResultDto> EmitirFacturaCAsync(
        EmitInvoiceDto request,
        decimal importe,
        string descripcion,
        DateTime fecha);
}
