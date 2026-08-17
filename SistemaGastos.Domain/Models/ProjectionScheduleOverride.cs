namespace SistemaGastos.Domain.Models;

public class ProjectionScheduleOverride
{
    public int ID { get; set; }
    public int UserID { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public long SourceID { get; set; }
    public string YearMonth { get; set; } = string.Empty;
    public int OriginalDay { get; set; }
    public int TargetDay { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
