using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.DTOs;

public record SaveBudgetDto(int ID, int CategoryID, decimal AmountLimit, int PeriodMonth, int PeriodYear);

public record BudgetFormDto(Budget? BudgetData, List<BudgetCategoryLookupDto> Categories, int Month, int Year);

public record BudgetCategoryLookupDto(int ID, string Name);

public record BudgetDetailItemDto(DateTime Date, string Description, string Account, decimal Amount, bool IsCard);

public record BudgetDetailsResult(List<BudgetDetailItemDto> Items, string CategoryName, string Period);
