using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaGastos.Application.ViewModels
{
    public class BudgetStatusViewModel
    {
        public int ID { get; set; }
        public string CategoryName { get; set; }
        public string CategoryIcon { get; set; } // Ej: fa-solid fa-burger
        public string CategoryColor { get; set; } // Ej: #ff0000

        public decimal Limit { get; set; }
        public decimal Spent { get; set; }
        public decimal Remaining => Limit - Spent;

        // Lógica de porcentaje (Tope 100% visual para no romper la gráfica)
        public int Percentage => Limit == 0 ? 100 : (int)((Spent / Limit) * 100);
        public int PercentageVisual => Percentage > 100 ? 100 : Percentage;

        // Semáforo de colores
        public string StatusColor => Percentage >= 100 ? "bg-danger" :
                                     Percentage >= 80 ? "bg-warning" : "bg-success";

        public string TextColor => Percentage >= 100 ? "text-danger" :
                                   Percentage >= 80 ? "text-warning" : "text-success";

        public decimal ProjectedAmount { get; set; } // Cuánto gastarás a fin de mes a este ritmo
        public bool ShowPacingAlert { get; set; }    // ¿Debemos mostrar la alerta?
        public decimal PacingDeviation { get; set; } // Diferencia (Proyección - Límite)
    }
}
