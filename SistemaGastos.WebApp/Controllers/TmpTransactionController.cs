using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.TmpTransactions.Commands;
using SistemaGastos.Application.Features.TmpTransactions.Queries;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Application.Wrappers;

namespace SistemaGastos.Controllers;

[Authorize]
public class TmpTransactionController(IMediator mediator, ICurrentUserService currentUserService) : Controller
{
    // GET: /TmpTransaction
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userID = currentUserService.UserId ?? 0;
        var stats = await mediator.Send(new GetDashboardStatsQuery(userID));

        ViewBag.TotalARSValue = stats.TotalARSValue;
        ViewBag.MonthlyBalances = stats.MonthlyBalances;
        ViewBag.Months = stats.Months;

        return View();
    }

    // GET: /TmpTransaction/GetTransactionsByMonth?year=2026&month=1
    [HttpGet]
    [Route("GetTransactionsByMonth")]
    public async Task<ActionResult<Response<List<TmpTransactionDto>>>> GetTransactionsByMonth(int year, int month)
    {
        var userID = currentUserService.UserId ?? 0;
        var transactions = await mediator.Send(new GetTransactionsByMonthQuery(userID, year, month));

        return Ok(Response<List<TmpTransactionDto>>.Ok(transactions));
    }

    // POST: /TmpTransaction/Create
    [HttpPost]
    [Route("Create")]
    public async Task<ActionResult<Response<object>>> Create([FromBody] CreateTmpTransactionRequest request)
    {
        var userID = currentUserService.UserId ?? 0;

        // Si es edición
        if (request.ID > 0)
        {
            var updateCommand = new UpdateTmpTransactionCommand
            {
                ID = request.ID,
                UserID = userID,
                Description = request.Description,
                Amount = request.Amount,
                Currency = request.Currency ?? "ARS",
                CategoryID = request.CategoryID,
                AccountID = request.AccountID,
                DateTransaction = request.DateTransaction,
                DistributionEndDay = request.DistributionEndDay,
                ExcludedDays = request.ExcludedDays
            };

            var success = await mediator.Send(updateCommand);

            if (success)
                return Ok(Response<object>.Ok(new { id = request.ID }, "Transacción actualizada correctamente"));

            return Ok(Response<object>.Fail("Transacción no encontrada"));
        }

        // Si es creación
        var createCommand = new CreateTmpTransactionCommand
        {
            UserID = userID,
            Description = request.Description,
            Amount = request.Amount,
            Currency = request.Currency ?? "ARS",
            CategoryID = request.CategoryID,
            AccountID = request.AccountID,
            DateTransaction = request.DateTransaction,
            EsRecurrente = request.EsRecurrente,
            MesesSeleccionados = request.MesesSeleccionados ?? new List<string>(),
            DistributionEndDay = request.DistributionEndDay,
            ExcludedDays = request.ExcludedDays
        };

        var created = await mediator.Send(createCommand);

        if (created > 0)
        {
            var message = created == 1
                ? "Transacción creada correctamente"
                : $"{created} transacciones creadas correctamente";
            return Ok(Response<object>.Ok(new { created }, message));
        }

        return Ok(Response<object>.Fail("No se pudo crear la transacción"));
    }

    // DELETE: /TmpTransaction/Delete
    [HttpDelete]
    [Route("Delete")]
    public async Task<ActionResult<Response<object>>> Delete([FromBody] List<long> ids)
    {
        var userID = currentUserService.UserId ?? 0;
        var command = new DeleteTmpTransactionsCommand(ids, userID);
        var deleted = await mediator.Send(command);

        if (deleted > 0)
            return Ok(Response<object>.Ok(new { deleted }, $"{deleted} transacciones eliminadas"));

        return Ok(Response<object>.Fail("No se encontraron transacciones para eliminar"));
    }

    // GET: /TmpTransaction/GetTmpTransactionForm?id=1
    [HttpGet]
    [Route("GetTmpTransactionForm")]
    public async Task<IActionResult> GetTmpTransactionForm(long? id)
    {
        var userID = currentUserService.UserId ?? 0;

        if (userID == 0)
        {
            return Unauthorized("Usuario no autenticado");
        }

        var formData = await mediator.Send(new GetTmpTransactionFormQuery(userID, id));
        return PartialView("_TmpTransactionForm", formData);
    }

    // GET: /TmpTransaction/GetBalances
    [HttpGet]
    [Route("GetBalances")]
    public async Task<ActionResult<Response<List<MonthlyBalanceDto>>>> GetBalances()
    {
        var userID = currentUserService.UserId ?? 0;
        var balances = await mediator.Send(new GetProjectedBalancesQuery(userID));
        return Ok(Response<List<MonthlyBalanceDto>>.Ok(balances));
    }

    // GET: /TmpTransaction/GetDailyBalances?year=2026&month=6
    [HttpGet]
    [Route("GetDailyBalances")]
    public async Task<ActionResult<Response<DailyCalendarDto>>> GetDailyBalances(int year, int month)
    {
        var userID = currentUserService.UserId ?? 0;
        var data = await mediator.Send(new GetDailyBalancesQuery(userID, year, month));
        return Ok(Response<DailyCalendarDto>.Ok(data));
    }

    // POST: /TmpTransaction/ConfirmDay
    [HttpPost]
    [Route("ConfirmDay")]
    public async Task<ActionResult<Response<bool>>> ConfirmDay([FromBody] ConfirmDayRequest request)
    {
        var userID = currentUserService.UserId ?? 0;
        var success = await mediator.Send(new ConfirmTmpTransactionDayCommand(request.ID, request.Day, userID));

        if (success)
            return Ok(Response<bool>.Ok(true, "Movimiento confirmado y registrado correctamente"));

        return Ok(Response<bool>.Fail("No se pudo confirmar el movimiento"));
    }

    // GET: /TmpTransaction/PlanMetas
    [HttpGet]
    public IActionResult PlanMetas() => View();

    // GET: /TmpTransaction/GetDebtPlanData
    [HttpGet]
    [Route("GetDebtPlanData")]
    public async Task<ActionResult<Response<DebtPlanDataDto>>> GetDebtPlanData()
    {
        var userID = currentUserService.UserId ?? 0;
        var data = await mediator.Send(new GetDebtPlanDataQuery(userID));
        return Ok(Response<DebtPlanDataDto>.Ok(data));
    }

    // POST: /TmpTransaction/SaveDebtPlanSettings
    [HttpPost]
    [Route("SaveDebtPlanSettings")]
    public async Task<ActionResult<Response<bool>>> SaveDebtPlanSettings([FromBody] DebtPlanSettingsDto settings)
    {
        var userID = currentUserService.UserId ?? 0;
        var ok = await mediator.Send(new SaveDebtPlanSettingsCommand(userID, settings));
        return Ok(Response<bool>.Ok(ok));
    }
}

// DTO para recibir el request del frontend
public class CreateTmpTransactionRequest
{
    public long ID { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ARS";
    public int CategoryID { get; set; }
    public int? AccountID { get; set; }
    public DateTime? DateTransaction { get; set; }
    public bool EsRecurrente { get; set; }
    public List<string> MesesSeleccionados { get; set; }
    public int? DistributionEndDay { get; set; }
    public string? ExcludedDays { get; set; }
}

public class ConfirmDayRequest
{
    public long ID { get; set; }
    public int Day { get; set; }
}
