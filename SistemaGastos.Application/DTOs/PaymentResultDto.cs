namespace SistemaGastos.Application.DTOs;

public class PaymentResultDto
{
    public int TransactionID { get; set; }
    public decimal Amount { get; set; }
    public string AccountName { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Message { get; set; }
}
