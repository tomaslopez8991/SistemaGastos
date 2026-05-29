namespace SistemaGastos.Application.DTOs;

public record InvoicePreviewDto(
    string RazonSocial,
    string Cuit,
    string Domicilio,
    string PuntoVenta,
    bool IsConfigured,
    string TipoComprobante,
    string Fecha,
    string Descripcion,
    decimal Importe,
    int TransactionId
);

public record EmitInvoiceDto(
    int TransactionId,
    // Receptor (ingresado en pantalla)
    string ReceptorNombre,
    string ReceptorCuit,
    // Emisor (pre-cargado desde config, editable en pantalla)
    string EmisorRazonSocial,
    string EmisorCuit,
    string EmisorDomicilio,
    string PuntoVenta
);

public record EmitInvoiceResultDto(
    bool Success,
    string? Cae,
    string? CaeVencimiento,
    string? Message
);
