using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaGastos.Application.ViewModels
{
    public class MultipleCreditCardTransactionViewModel
    {
        public int AccountID { get; set; }
        [DataType(DataType.Date)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime Date { get; set; }
        public int CategoryID { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public int Installments { get; set; }
        public int ActualInstallment { get; set; }
        public bool Fixed { get; set; }
    }
}
