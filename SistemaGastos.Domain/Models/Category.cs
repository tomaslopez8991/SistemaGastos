using System.ComponentModel.DataAnnotations;

namespace SistemaGastos.Domain.Models
{
    public class Category
    {
        [Key]
        public int ID { get; set; }
        [Required(ErrorMessage = "Debe ingresar un nombre para la categoría")]
        public string Name { get; set; }
        public string? Description { get; set; }
        [Required(ErrorMessage = "Debe seleccionar un tipo para la categoría")]
        public string Type { get; set; }
        [Required(ErrorMessage = "Debe seleccionar un ícono para la categoría")]
        public string Icon { get; set; }
        [Required(ErrorMessage = "Debe seleccionar un color para la categoría")]
        [StringLength(7)]
        public string Color { get; set; }
    }
}
