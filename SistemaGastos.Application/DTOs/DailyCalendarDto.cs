namespace SistemaGastos.Application.DTOs;

public class DailyCalendarDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthLabel { get; set; }

    /// <summary>Saldo acumulado al cierre del mes anterior (punto de partida del mes).</summary>
    public decimal StartingBalance { get; set; }
    public string StartingBalanceFmt { get; set; }

    /// <summary>Cotización dólar MEP usada para convertir montos en USD.</summary>
    public decimal DolarRate { get; set; }

    public List<DailyBalanceDto> Days { get; set; } = new();
}

public class DailyBalanceDto
{
    public int Day { get; set; }
    /// <summary>Fecha en formato yyyy-MM-dd.</summary>
    public string Date { get; set; }

    public decimal Income { get; set; }
    public decimal Expense { get; set; }

    /// <summary>Saldo acumulado al cierre de este día.</summary>
    public decimal Balance { get; set; }
    public string BalanceFmt { get; set; }

    public List<DailyBalanceItemDto> Items { get; set; } = new();
}

public class DailyBalanceItemDto
{
    public string Description { get; set; }
    /// <summary>Monto convertido a ARS.</summary>
    public decimal Amount { get; set; }
    public string AmountFmt { get; set; }
    public bool IsIncome { get; set; }
    /// <summary>Planificado, GastoFijo, IngresoFijo, TarjetaCredito, Personas.</summary>
    public string SourceType { get; set; }
}
