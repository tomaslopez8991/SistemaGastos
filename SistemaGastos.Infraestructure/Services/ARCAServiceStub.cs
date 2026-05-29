using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Infraestructure.Services;

/// <summary>
/// Implementación stub del servicio ARCA (AFIP).
/// Simula la emisión de comprobantes para entornos de desarrollo o cuando no hay credenciales configuradas.
/// Reemplazar por la implementación real (WSFE/WSAA) cuando se disponga de certificado digital ARCA.
/// </summary>
public class ARCAServiceStub : IARCAService
{
    public bool IsConfigured => false;

    public Task<EmitInvoiceResultDto> EmitirFacturaCAsync(
        EmitInvoiceDto request,
        decimal importe,
        string descripcion,
        DateTime fecha)
    {
        var caeSimulado = $"CAE{DateTime.Now:yyyyMMddHHmmss}";
        var vencimientoCae = DateTime.Today.AddDays(10).ToString("dd/MM/yyyy");

        var result = new EmitInvoiceResultDto(
            Success: true,
            Cae: caeSimulado,
            CaeVencimiento: vencimientoCae,
            Message: "Factura emitida en modo simulado. Servicio ARCA no configurado."
        );

        return Task.FromResult(result);
    }
}
