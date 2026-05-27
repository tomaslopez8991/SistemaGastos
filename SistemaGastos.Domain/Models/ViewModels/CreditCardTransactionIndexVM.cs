using SistemaGastos.Domain.Models;

namespace SistemaGastos.WebApp.Models.ViewModels;

public class CreditCardTransactionIndexVM
{
    public List<Account> Accounts { get; set; } = [];
    public List<Category> Categories { get; set; } = [];
}