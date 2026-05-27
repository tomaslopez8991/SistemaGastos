using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaGastos.Application.DTOs
{
    public class BalanceBreakdownDto
    {
        public decimal InitialBalance { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal ManualExpenses { get; set; }
        public decimal FixedExpenses { get; set; }
        public decimal CreditCardInstallments { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal FinalBalance { get; set; }
        public List<FixedExpenseDetailDto> FixedExpensesDetails { get; set; } = new();
    }
}
