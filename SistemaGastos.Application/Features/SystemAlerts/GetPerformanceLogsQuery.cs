using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Application.Features.SystemAlerts;

public record PerformanceLogDto(int ID, string HandlerName, long ElapsedMs, string? RequestData, DateTime CreatedAt);

public record GetPerformanceLogsQuery(int Page, int PageSize) : IRequest<(List<PerformanceLogDto> Items, int Total)>;

public class GetPerformanceLogsHandler(IApplicationDbContext context)
    : IRequestHandler<GetPerformanceLogsQuery, (List<PerformanceLogDto> Items, int Total)>
{
    public async Task<(List<PerformanceLogDto> Items, int Total)> Handle(GetPerformanceLogsQuery request, CancellationToken cancellationToken)
    {
        var query = context.PerformanceLog.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new PerformanceLogDto(x.ID, x.HandlerName, x.ElapsedMs, x.RequestData, x.CreatedAt))
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
