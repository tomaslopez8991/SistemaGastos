using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaGastos.Application.ViewModels
{
    public class UserVM
    {
        public int ID { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; } // Opcional, para mostrar antigüedad
    }
    public class ChangePasswordVM
    {
        public int UserID { get; set; }
        public string CurrentPassword { get; set; } // Opcional si quieres validar la anterior
        public string NewPassword { get; set; }

    }
}