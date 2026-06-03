namespace SistemaGastos.Application.DTOs;

public class PersonDto
{
    public int ID { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
}

public class PersonAccountItemDto
{
    public string Description { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public decimal Amount { get; set; }
    public string AmountFmt { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string TypeLabel { get; set; } = string.Empty;
    public decimal Percentage { get; set; } = 100;
    public DateTime Date { get; set; }
    public string DateFmt { get; set; } = string.Empty;
}

public class PersonAccountDto
{
    public int PersonID { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public decimal TotalOwed { get; set; }
    public string TotalOwedFmt { get; set; } = string.Empty;
    public List<PersonAccountItemDto> Items { get; set; } = new();
}
