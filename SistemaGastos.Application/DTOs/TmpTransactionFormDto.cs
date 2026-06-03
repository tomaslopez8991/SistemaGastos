using Microsoft.AspNetCore.Mvc.Rendering;

namespace SistemaGastos.Application.DTOs;

public class TmpTransactionFormDto
{
    public TmpTransactionDto Transaction { get; set; }
    public List<SelectListItem> Categories { get; set; }
    public List<SelectListItem> Accounts { get; set; }
    public List<MonthSelectionDto> NextMonths { get; set; }
    /// <summary>Cotización dólar MEP al momento de abrir el formulario.</summary>
    public decimal DolarMep { get; set; }
}

public class MonthSelectionDto
{
    public string Value { get; set; }
    public string Text { get; set; }
}