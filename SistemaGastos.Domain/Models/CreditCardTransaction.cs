using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaGastos.Domain.Models
{
    public class CreditCardTransaction
    {
        [Key]
        public int ID { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime TransactionDate { get; set; }

        public int? ActualInstallment { get; set; }
        public int? Installments { get; set; }
        public bool Fixed { get; set; }

        //Foreign keys
        public int CategoryID { get; set; }
        [ForeignKey("CategoryID")]
        public virtual Category? Category { get; set; }

        public int AccountID { get; set; }
        [ForeignKey("AccountID")]
        public virtual Account? Account { get; set; }

        // ✅ NUEVO: Relación con FixedExpense
        public int? FixedExpenseID { get; set; }
        [ForeignKey("FixedExpenseID")]
        public virtual FixedExpense? FixedExpense { get; set; }
    }
}
