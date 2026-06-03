namespace SistemaGastos.Application.DTOs;

public class FixedExpenseFormDto
{
    public FixedExpenseDto Expense { get; set; }
    public List<AccountDropdownDto> Accounts { get; set; }
    public List<CategoryDropdownDto> Categories { get; set; }
    public List<PersonDropdownDto> Persons { get; set; } = new();
}

public class PersonDropdownDto
{
    public int ID { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class AccountDropdownDto
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
}

public class CategoryDropdownDto
{
    public int ID { get; set; }
    public string Name { get; set; }
}
