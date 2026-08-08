using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.ViewModels;

public class CreditCardTransactionIndexVM
{
    public List<Account> Accounts { get; set; } = [];
    public List<Category> Categories { get; set; } = [];
    public List<Person> Persons { get; set; } = [];
}
