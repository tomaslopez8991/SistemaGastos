using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaGastos.Domain.Models.DTOs
{
    public class TransactionBulkDTO
    {
        public int AccountID { get; set; }
        public int CategoryID { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
    }
}
