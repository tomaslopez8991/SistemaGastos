using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SistemaGastos.Domain.Enums;

namespace SistemaGastos.Domain.Models
{
    public class Account
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public string Currency { get; set; }

        [Display(Name = "Current Balance")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal Balance { get; set; }

        public string DisplayName => $"{Name} ({Currency})";

        //Foreign keys
        public int UserID { get; set; }
        [ForeignKey("UserID")]
        public virtual Login? Login { get; set; }

        // ✅ CAMBIO: De string a Enum
        public AccountType Type { get; set; }

        public int? ClosingDay { get; set; }
        public int? DueDay { get; set; }

        /// <summary>Meses entre el cierre del resumen y su vencimiento (0 = mismo mes, 1 = mes siguiente, etc). Default: 1.</summary>
        public int? DueMonthOffset { get; set; }

        /// <summary>Porcentaje del saldo usado para calcular el pago mínimo sugerido (ej. 15 = 15%).</summary>
        public decimal? MinimumPaymentPercentage { get; set; }

        /// <summary>Monto de pago mínimo cargado manualmente, pisa el cálculo automático cuando tiene valor.</summary>
        public decimal? MinimumPaymentManualOverride { get; set; }

        /// <summary>Pago mínimo efectivo: usa el override manual si está cargado, o lo calcula como % del saldo.</summary>
        public decimal? EffectiveMinimumPayment =>
            MinimumPaymentManualOverride
            ?? (MinimumPaymentPercentage.HasValue ? Math.Abs(Balance) * MinimumPaymentPercentage.Value / 100m : null);

        /// <summary>Modo de proyección para el pago TC en el cashflow. Default: Total.</summary>
        public TcProjectionMode TcProjectionMode { get; set; } = TcProjectionMode.Total;

        /// <summary>Monto personalizado a proyectar cuando TcProjectionMode = Personalizado.</summary>
        public decimal? TcCustomPaymentAmount { get; set; }

        /// <summary>Monto efectivo a proyectar según el modo configurado.</summary>
        public decimal EffectiveTcProjection =>
            TcProjectionMode switch
            {
                TcProjectionMode.Minimo       => EffectiveMinimumPayment ?? Math.Abs(Balance),
                TcProjectionMode.Personalizado => TcCustomPaymentAmount   ?? Math.Abs(Balance),
                _                             => Math.Abs(Balance)
            };

        public ICollection<CreditCardTransaction> CreditCardTransactions { get; set; } = new List<CreditCardTransaction>();
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<FixedExpense> FixedExpenses { get; set; } = new List<FixedExpense>();
    }
}
