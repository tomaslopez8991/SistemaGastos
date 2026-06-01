using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SistemaGastos.Domain.Enums;

namespace SistemaGastos.Domain.Models
{
    public class Transaction
    {
        [Key]
        public int ID { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime Date { get; set; }

        public string Description { get; set; }

        [Display(Name = "Amount")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal Amount { get; set; }

        //Foreign keys
        public int AccountID { get; set; }
        public int CategoryID { get; set; }
        public int? FixedExpenseID { get; set; }

        [ForeignKey("AccountID")]
        public virtual Account? Account { get; set; }

        [ForeignKey("CategoryID")]
        public virtual Category? Category { get; set; }

        [ForeignKey("FixedExpenseID")]
        public virtual FixedExpense? FixedExpense { get; set; }

        public int? PersonID { get; set; }
        [ForeignKey("PersonID")]
        public virtual Person? Person { get; set; }
    }
}
