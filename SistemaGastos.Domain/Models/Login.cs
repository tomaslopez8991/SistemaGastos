using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SistemaGastos.Domain.Models
{
    public class Login
    {
        [Key]
        public int ID { get; set; }
        [Required(ErrorMessage = "Debe ingresar un usuario.")]
        [MaxLength(50)]
        public string Username { get; set; }
        [Required(ErrorMessage = "Debe ingresar una contraseña.")]
        [MaxLength(100)]
        public string Password { get; set; }
        [EmailAddress]
        [Required(ErrorMessage = "Debe ingresar un correo electrónico.")]
        [MaxLength(100)]
        public string Email { get; set; }
        [DefaultValue("User")]
        [MaxLength(50)]
        public string? Role { get; set; }
        [DefaultValue(false)]
        public bool Active { get; set; }

        public bool EmailConfirmed { get; set; }
        [MaxLength(64)]
        public string? EmailConfirmationTokenHash { get; set; }
        public DateTime? EmailConfirmationTokenExpiry { get; set; }
        public DateTime? ConfirmationEmailSentAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
    }
}
