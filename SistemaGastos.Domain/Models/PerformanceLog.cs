namespace SistemaGastos.Domain.Models;

public class PerformanceLog
{
    public int ID { get; set; }
    public string HandlerName { get; set; } = string.Empty;
    public long ElapsedMs { get; set; }
    public string? RequestData { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
