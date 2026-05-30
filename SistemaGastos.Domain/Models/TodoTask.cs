using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaGastos.Domain.Models
{
    public class TodoTask
    {
        [Key]
        public int TaskId { get; set; }

        [Required]
        public string Title { get; set; }

        public string? Description { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime DueDate { get; set; }

        public bool IsCompleted { get; set; }

        /// <summary>1 = Alta, 2 = Media, 3 = Baja</summary>
        public int Priority { get; set; } = 2;

        public DateTime? ReminderDate { get; set; }

        //Foreign keys
        public int UserID { get; set; }
        [ForeignKey("UserID")]
        public virtual Login? Login { get; set; }
    }
}
