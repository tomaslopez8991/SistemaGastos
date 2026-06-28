namespace SistemaGastos.Application.DTOs;

public class DashboardVM
{
    public decimal SaldoTotal { get; set; }
    public decimal GastosMes { get; set; }
    public decimal IngresosMes { get; set; }
    public decimal BalanceMes => IngresosMes - GastosMes;
    public decimal DeudaTarjetas { get; set; }
    public int TareasPendientes { get; set; }
    public List<ProximoGastoFijoDto> ProximosGastosFijos { get; set; } = [];
}

public record ProximoGastoFijoDto(string Name, decimal Amount, string Currency, int PaymentDay, string? LogoUrl);
