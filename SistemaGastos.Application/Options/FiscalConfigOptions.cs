namespace SistemaGastos.Application.Options;

public class FiscalConfigOptions
{
    public string RazonSocial { get; set; } = string.Empty;
    public string Cuit { get; set; } = string.Empty;
    public string Domicilio { get; set; } = string.Empty;
    public string PuntoVenta { get; set; } = "0001";
}
