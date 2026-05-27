namespace SistemaGastos.Application.DTOs;

public class TmpTransactionDto
{
    public long ID { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public int CategoryID { get; set; }
    public string CategoryName { get; set; }
    public string CategoryType { get; set; } // "Ingreso" o "Gasto"
    public int? AccountID { get; set; }
    public string AccountName { get; set; }
    public DateTime? DateTransaction { get; set; }
    public bool EsRecurrente { get; set; }
    public string AmountFormatted { get; set; }
    public bool IsIngreso { get; set; }
    public bool IsPaid { get; set; }
}