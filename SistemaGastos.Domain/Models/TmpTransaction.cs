using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaGastos.Domain.Models
{
    public class TmpTransaction
    {
        [Key]
        public int ID { get; set; }
        public string Description { get; set; }
        [Display(Name = "Amount")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal Amount { get; set; }
        
        [DataType(DataType.Date)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime? DateTransaction { get; set; }
        public bool? EsRecurrente { get; set; }
        /// <summary>"ARS" o "USD". El Amount se almacena en la moneda original.</summary>
        public string Currency { get; set; } = "ARS";
        [NotMapped]
        public List<string>? MesesSeleccionados { get; set; }

        //Foreign keys
        public int AccountID { get; set; }
        public int CategoryID { get; set; }
        public int UserID { get; set; }
        //public int? FixedExpenseID { get; set; }

        [ForeignKey("AccountID")]
        public virtual Account? Account { get; set; }
        [ForeignKey("CategoryID")]
        public virtual Category? Category { get; set; }
        [ForeignKey("UserID")]
        public virtual Login? Login { get; set; }
        //[ForeignKey("FixedExpenseID")]
        //public virtual FixedExpense? FixedExpense { get; set; }
    }
}
