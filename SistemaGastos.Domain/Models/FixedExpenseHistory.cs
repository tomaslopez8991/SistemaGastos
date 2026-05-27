using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaGastos.Domain.Models
{
    public class FixedExpenseHistory
    {
        [Key]
        public int ID { get; set; }

        public DateTime ChangeDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal OldAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NewAmount { get; set; }

        public int FixedExpenseID { get; set; }

        [ForeignKey("FixedExpenseID")]
        public virtual FixedExpense FixedExpense { get; set; }
    }
}
