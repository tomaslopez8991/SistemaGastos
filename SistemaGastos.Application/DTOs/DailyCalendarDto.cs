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

    /// <summary>True si el mes solicitado es anterior al mes actual.</summary>
    public bool IsPastMonth { get; set; }

    /// <summary>True si existe al menos un ítem pendiente en el mes pasado solicitado.</summary>
    public bool HasPendingItems { get; set; }

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
    /// <summary>ID del registro origen (solo para SourceType = Planificado).</summary>
    public long? SourceId { get; set; }
    public string Description { get; set; }
    /// <summary>Monto convertido a ARS.</summary>
    public decimal Amount { get; set; }
    public string AmountFmt { get; set; }
    public bool IsIncome { get; set; }
    /// <summary>Planificado, GastoFijo, IngresoFijo, TarjetaCredito, Personas.</summary>
    public string SourceType { get; set; }
    /// <summary>Día del mes al que pertenece este ítem (para overrides por día).</summary>
    public int Day { get; set; }
    /// <summary>True si el TmpTransaction tiene distribución activa (dist.Count > 1).</summary>
    public bool IsDistributed { get; set; }
    /// <summary>ID de la cuenta TC (para items de pago de TC).</summary>
    public int? TcAccountId { get; set; }
    /// <summary>Saldo total de la TC en ARS (para botón "Pagar total").</summary>
    public decimal? TcTotalAmount { get; set; }
    /// <summary>Pago mínimo en ARS (null si no configurado).</summary>
    public decimal? TcMinimumAmount { get; set; }
    public bool IsAutomaticPersonCollection { get; set; }
    public string? TcProjectionMode { get; set; }
    public decimal? TcCustomAmount { get; set; }
}
