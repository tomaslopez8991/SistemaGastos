using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.ViewModels
{
    public class TransactionVM
    {
        public List<Transaction> Transactions { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? FirstTransaction {  get; set; }
        public int? AccountID { get; set; }
        public int? CategoryID { get; set; }
        public virtual Account Account { get; set; }
        public virtual Category Category { get; set; }
    }
}
