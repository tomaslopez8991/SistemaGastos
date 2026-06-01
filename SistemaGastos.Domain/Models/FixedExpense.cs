using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaGastos.Domain.Models
{
    public class FixedExpense
    {
        public int ID { get; set; }
        public int UserID { get; set; }
        public int AccountID { get; set; }
        public int CategoryID { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int PaymentDay { get; set; }
        public string? LogoUrl { get; set; }
        public bool Active { get; set; }
        public DateTime? LastGeneratedDate { get; set; }

        public int? PersonID { get; set; }

        public virtual Login User { get; set; } = null!;
        public virtual Account Account { get; set; } = null!;
        public virtual Category Category { get; set; } = null!;
        public virtual Person? Person { get; set; }
    }
}