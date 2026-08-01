using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaGastos.Domain.Models
{
    public class Person
    {
        [Key]
        public int ID { get; set; }
        public int UserID { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Active { get; set; } = true;

        [ForeignKey("UserID")]
        public virtual Login User { get; set; } = null!;

        public int? CollectionDay { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? CollectedMonths { get; set; }
        public string? CollectionFrom { get; set; }

        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public virtual ICollection<CreditCardTransaction> CreditCardTransactions { get; set; } = new List<CreditCardTransaction>();
        public virtual ICollection<FixedExpense> FixedExpenses { get; set; } = new List<FixedExpense>();
        public virtual ICollection<FixedIncome> FixedIncomes { get; set; } = new List<FixedIncome>();
    }
}
