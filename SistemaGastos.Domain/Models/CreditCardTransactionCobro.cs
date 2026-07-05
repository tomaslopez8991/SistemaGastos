using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaGastos.Domain.Models;

public class CreditCardTransactionCobro
{
    [Key] public int ID { get; set; }
    public int PersonID { get; set; }
    [ForeignKey("PersonID")] public virtual Person? Person { get; set; }
    public int CreditCardTransactionID { get; set; }
    [ForeignKey("CreditCardTransactionID")] public virtual CreditCardTransaction? CreditCardTransaction { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
