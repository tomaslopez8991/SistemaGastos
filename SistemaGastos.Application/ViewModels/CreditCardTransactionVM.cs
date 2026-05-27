using System.ComponentModel.DataAnnotations;

namespace SistemaGastos.Application.ViewModels
{
    public class CreditCardTransactionVM
    {
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public int ActualInstallment { get; set; }
        public int Installments { get; set; }
        public string? Currency { get; set; }
        [DataType(DataType.Date)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime TransactionDate { get; set; }
        public int CategoryId { get; set; }
        public int AccountId { get; set; }
        public bool Fixed { get; set; }
    }
}
