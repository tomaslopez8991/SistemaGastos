namespace SistemaGastos.Application.Helpers;

public static class DistributionHelper
{
    public static List<int> ParseExcludedDays(string? excludedDays)
    {
        if (string.IsNullOrWhiteSpace(excludedDays)) return new List<int>();
        return excludedDays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var d) ? d : -1)
            .Where(d => d is >= 1 and <= 31)
            .Distinct()
            .ToList();
    }

    public static string? SerializeExcludedDays(IEnumerable<int> days)
    {
        var list = days.Where(d => d is >= 1 and <= 31).Distinct().OrderBy(d => d).ToList();
        return list.Count == 0 ? null : string.Join(",", list);
    }

    /// <summary>
    /// Reparte totalAmount entre los días activos de [startDay, endDay] (clamp a daysInMonth),
    /// salteando excludedDays. El último día activo absorbe el resto del redondeo.
    /// Si no hay rango real o no quedan días activos, todo va a startDay.
    /// </summary>
    public static Dictionary<int, decimal> Distribute(decimal totalAmount, int startDay, int? endDay, List<int> excludedDays, int daysInMonth)
    {
        if (endDay is null || endDay <= startDay)
            return new Dictionary<int, decimal> { [startDay] = totalAmount };

        var clampedEnd = Math.Min(endDay.Value, daysInMonth);
        var activeDays = Enumerable.Range(startDay, clampedEnd - startDay + 1)
            .Where(d => !excludedDays.Contains(d))
            .ToList();

        if (activeDays.Count == 0)
            return new Dictionary<int, decimal> { [startDay] = totalAmount };

        var result = new Dictionary<int, decimal>();
        var perDay = Math.Round(totalAmount / activeDays.Count, 2, MidpointRounding.AwayFromZero);
        decimal assigned = 0;
        for (int i = 0; i < activeDays.Count; i++)
        {
            var amount = (i == activeDays.Count - 1) ? totalAmount - assigned : perDay;
            result[activeDays[i]] = amount;
            assigned += amount;
        }
        return result;
    }
}
