using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaGastos.Application.DTOs
{
    public class FixedExpenseDetailDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Category { get; set; } = string.Empty;
        public int PaymentDay { get; set; }
        public DateTime NextPaymentDate { get; set; }
    }
}
