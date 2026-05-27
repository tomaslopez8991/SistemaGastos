namespace SistemaGastos.Application.DTOs;

public class DashboardVM
{
    public decimal SaldoTotal { get; set; }
    public decimal GastosMes { get; set; }
    public decimal DeudaTarjetas { get; set; }
    public int TareasPendientes { get; set; }
}