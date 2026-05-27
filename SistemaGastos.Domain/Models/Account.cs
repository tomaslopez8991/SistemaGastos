using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SistemaGastos.Domain.Enums;

namespace SistemaGastos.Domain.Models
{
    public class Account
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public string Currency { get; set; }

        [Display(Name = "Current Balance")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal Balance { get; set; }

        public string DisplayName => $"{Name} ({Currency})";

        //Foreign keys
        public int UserID { get; set; }
        [ForeignKey("UserID")]
        public virtual Login? Login { get; set; }

        // ✅ CAMBIO: De string a Enum
        public AccountType Type { get; set; }

        public int? ClosingDay { get; set; }
        public int? DueDay { get; set; }

        public ICollection<CreditCardTransaction> CreditCardTransactions { get; set; } = new List<CreditCardTransaction>();
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<FixedExpense> FixedExpenses { get; set; } = new List<FixedExpense>();
    }
}
