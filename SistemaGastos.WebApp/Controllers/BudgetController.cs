using Microsoft.AspNetCore.Authorization;

namespace SistemaGastos.Controllers;

[Authorize]
public class BudgetController(ApplicationDbContext context, ICurrentUserService currentUser) : Controller
{
    public async Task<IActionResult> GetBudgetTab(int month, int year)
    {
        var username = currentUser.Username;
        if (username == null) return RedirectToAction("Index", "Login");

        // 1. Definir Periodo (Default: Hoy)
        ViewBag.CurrentMonth = month;
        ViewBag.CurrentYear = year;
        ViewBag.DateLabel = new DateTime(year, month, 1).ToString("MMMM yyyy").ToUpper();

        // 2. Traer Presupuestos del Usuario para ese mes
        var budgets = await context.Budget
            .Include(b => b.Category)
            .Where(b => b.Login.Username == username && b.PeriodMonth == month && b.PeriodYear == year)
            .ToListAsync();

        // 3. Calcular Gastos Reales (Efectivo + Tarjeta)
        // Traemos todos los gastos del mes para no hacer mil consultas en el loop
        var cashExpenses = await context.Transaction
            .Where(t => t.Account.Login.Username == username && t.Date.Month == month && t.Date.Year == year)
            .ToListAsync();

        var cardExpenses = await context.CreditCardTransaction
            .Include(t => t.Account)
            .Where(t => t.Account.Login.Username == username && t.TransactionDate.Month == month && t.TransactionDate.Year == year)
            .ToListAsync();

        // 4. Mapear al ViewModel
        var model = new List<BudgetStatusViewModel>();
        var now = DateTime.Now;
        int daysInMonth = DateTime.DaysInMonth(year, month);

        foreach (var b in budgets)
        {
            var gastadoEfectivo = cashExpenses.Where(t => t.CategoryID == b.CategoryID).Sum(t => t.Amount);
            var gastadoTarjeta = cardExpenses.Where(t => t.CategoryID == b.CategoryID).Sum(t => t.Amount);
            decimal totalSpent = gastadoEfectivo + gastadoTarjeta;

            // --- CÁLCULO DE PACING ---
            decimal projected = totalSpent; // Por defecto es lo mismo
            bool showAlert = false;
            decimal deviation = 0;

            // Solo calculamos ritmo si es el mes ACTUAL y ya pasaron días (para evitar div/0 el día 1 a la madrugada)
            if (month == now.Month && year == now.Year && now.Day > 1)
            {
                // Ritmo = (Gastado / DíasPasados) * DíasTotales
                decimal dailyAverage = totalSpent / now.Day;
                projected = dailyAverage * daysInMonth;

                // Solo alertamos si la proyección supera el límite Y si todavía no nos hemos excedido realmente
                // (Si ya te excediste, la barra roja ya te avisa, no hace falta redundar)
                if (projected > b.AmountLimit && totalSpent <= b.AmountLimit)
                {
                    showAlert = true;
                    deviation = projected - b.AmountLimit;
                }
            }

            model.Add(new BudgetStatusViewModel
            {
                ID = b.ID,
                CategoryName = b.Category.Name,
                CategoryIcon = b.Category.Icon ?? "fa-solid fa-tag",
                CategoryColor = b.Category.Color ?? "#6c757d",
                Limit = b.AmountLimit,
                Spent = totalSpent,

                // Asignamos lo nuevo
                ProjectedAmount = projected,
                ShowPacingAlert = showAlert,
                PacingDeviation = deviation
            });
        }

        return PartialView("_BudgetCards", model);
    }

    [HttpGet]
    public async Task<IActionResult> GetBudgetForm(int? id, int? month, int? year)
    {
        ViewBag.Categories = await context.Category
            .Where(c => c.Type == "Gasto" || c.Type == "Salidas") // Solo gastos
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem { Value = c.ID.ToString(), Text = c.Name })
            .ToListAsync();

        if (id.HasValue)
        {
            var budget = await context.Budget.FindAsync(id);
            return PartialView("_BudgetForm", budget);
        }

        int m = month ?? DateTime.Now.Month;
        int y = year ?? DateTime.Now.Year;

        return PartialView("_BudgetForm", new Budget { PeriodMonth = m, PeriodYear = y });
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] Budget budget)
    {
        if (!ModelState.IsValid)
        {
            // Recopilamos los errores para mostrarlos claros en el SweetAlert
            var errors = string.Join("; ", ModelState.Values
                                            .SelectMany(v => v.Errors)
                                            .Select(e => e.ErrorMessage));
            return Json(new { success = false, message = "Datos inválidos: " + errors });
        }

        try
        {
            var username = currentUser.Username;
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false, message = "La sesión ha expirado." });

            var user = await context.Login.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return Json(new { success = false, message = "Usuario no encontrado." });

            budget.UserID = user.ID;

            var existe = await context.Budget
                .AnyAsync(b => b.UserID == user.ID &&
                               b.CategoryID == budget.CategoryID &&
                               b.PeriodMonth == budget.PeriodMonth &&
                               b.PeriodYear == budget.PeriodYear &&
                               b.ID != budget.ID);

            if (existe)
                return Json(new { success = false, message = "Ya tienes un presupuesto definido para esta categoría en ese mes." });

            if (budget.ID > 0)
            {
                context.Update(budget);
            }
            else
            {
                context.Add(budget);
            }

            await context.SaveChangesAsync();
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error interno al guardar el presupuesto." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete([FromBody] int id)
    {
        if (!ModelState.IsValid)
        {
            // Recopilamos los errores para mostrarlos claros en el SweetAlert
            var errors = string.Join("; ", ModelState.Values
                                            .SelectMany(v => v.Errors)
                                            .Select(e => e.ErrorMessage));
            return Json(new { success = false, message = "Datos inválidos: " + errors });
        }

        var b = await context.Budget.FindAsync(id);
        if (b != null) { context.Budget.Remove(b); await context.SaveChangesAsync(); }
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> CloneFromPreviousMonth(int targetMonth, int targetYear)
    {
        // 1. Calcular mes anterior
        var date = new DateTime(targetYear, targetMonth, 1).AddMonths(-1);
        int prevMonth = date.Month;
        int prevYear = date.Year;

        var username = currentUser.Username;
        if (string.IsNullOrEmpty(username))
            return Json(new { success = false, message = "La sesión ha expirado." });

        var user = await context.Login.FirstOrDefaultAsync(u => u.Username == username);

        // 2. Buscar presupuestos viejos
        var oldBudgets = await context.Budget
            .Where(b => b.UserID == user.ID && b.PeriodMonth == prevMonth && b.PeriodYear == prevYear)
            .AsNoTracking() // Importante para no copiar el ID
            .ToListAsync();

        if (!oldBudgets.Any()) return Json(new { success = false, message = "No hay nada que copiar del mes anterior." });

        // 3. Crear nuevos objetos
        var newBudgets = oldBudgets.Select(b => new Budget
        {
            UserID = b.UserID,
            CategoryID = b.CategoryID,
            AmountLimit = b.AmountLimit,
            PeriodMonth = targetMonth,
            PeriodYear = targetYear
        });

        context.Budget.AddRange(newBudgets);
        await context.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> GetDetails(int id)
    {
        var username = currentUser.Username;

        // 1. Buscamos el presupuesto para saber qué Categoría y qué Mes filtrar
        var budget = await context.Budget
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.ID == id);

        if (budget == null) return NotFound();

        // 2. Buscamos Gastos en Efectivo/Cuentas
        var cashTransactions = await context.Transaction
            .Include(t => t.Account)
            .Where(t => t.Account.Login.ID == budget.UserID &&
                        t.CategoryID == budget.CategoryID &&
                        t.Date.Month == budget.PeriodMonth &&
                        t.Date.Year == budget.PeriodYear)
            .Select(t => new
            {
                Date = t.Date,
                Description = t.Description,
                Account = t.Account.Name,
                Amount = t.Amount,
                IsCard = false
            })
            .ToListAsync();

        // 3. Buscamos Gastos con Tarjeta
        var cardTransactions = await context.CreditCardTransaction
            .Include(t => t.Account)
            .Where(t => t.Account.Login.Username == username && // Ajuste por relación
                        t.CategoryID == budget.CategoryID &&
                        t.TransactionDate.Month == budget.PeriodMonth &&
                        t.TransactionDate.Year == budget.PeriodYear)
            .Select(t => new
            {
                Date = t.TransactionDate,
                Description = t.Description,
                Account = t.Account.Name,
                Amount = t.Amount,
                IsCard = true
            })
            .ToListAsync();

        // 4. Unificamos y ordenamos por fecha descendente (lo más reciente arriba)
        var allDetails = cashTransactions
            .Concat(cardTransactions)
            .OrderByDescending(x => x.Date)
            .ToList();

        ViewBag.CategoryName = budget.Category.Name;
        ViewBag.Period = $"{budget.PeriodMonth}/{budget.PeriodYear}";

        return PartialView("_BudgetDetails", allDetails);
    }
}