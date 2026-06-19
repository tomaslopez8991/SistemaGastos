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
        /// <summary>"ARS" o "USD". El Amount se almacena en la moneda original.</summary>
        public string Currency { get; set; } = "ARS";
        public int PaymentDay { get; set; }
        public string? LogoUrl { get; set; }
        public bool Active { get; set; }
        /// <summary>Mes a partir del cual se incluye en proyecciones. Null = siempre activo.</summary>
        public DateTime? StartDate { get; set; }
        public DateTime? LastGeneratedDate { get; set; }

        public int? PersonID { get; set; }
        public decimal? PersonPercentage { get; set; }

        /// <summary>Día del mes (1-31) hasta el cual se reparte el monto. Null = sin distribución.</summary>
        public int? DistributionEndDay { get; set; }
        /// <summary>Días del mes (separados por coma) sin movimiento dentro del rango de distribución.</summary>
        public string? ExcludedDays { get; set; }
        /// <summary>Meses pausados (YYYY-MM separados por coma). Pausado = no aparece en proyecciones ese mes.</summary>
        public string? PausedMonths { get; set; }

        public virtual Login User { get; set; } = null!;
        public virtual Account Account { get; set; } = null!;
        public virtual Category Category { get; set; } = null!;
        public virtual Person? Person { get; set; }
    }
}