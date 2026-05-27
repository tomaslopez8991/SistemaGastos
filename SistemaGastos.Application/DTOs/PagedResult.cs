namespace SistemaGastos.Application.DTOs;

public class PagedResult<T>
{
    public List<T> Results { get; set; } = [];
    public int Total { get; set; }

    public PagedResult(List<T> results, int total)
    {
        Results = results;
        Total = total;
    }
}