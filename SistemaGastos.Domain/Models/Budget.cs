using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SistemaGastos.Domain.Models
{
    public class Budget
    {
        [Key]
        public int ID { get; set; }

        public int UserID { get; set; }

        [ForeignKey("UserID")]
        [ValidateNever]
        public Login Login { get; set; }

        [Required(ErrorMessage = "La categoría es obligatoria")]
        public int CategoryID { get; set; }

        [ForeignKey("CategoryID")]
        [ValidateNever]
        public Category Category { get; set; }

        [Required(ErrorMessage = "Debes definir un límite")]
        [Range(1, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountLimit { get; set; }

        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
    }
}