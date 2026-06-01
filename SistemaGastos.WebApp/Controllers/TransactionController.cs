using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Application.Features.Invoices.Commands;
using SistemaGastos.Application.Features.Invoices.Queries;
using SistemaGastos.Application.Features.Transactions.Commands;
using SistemaGastos.Application.Features.Transactions.Queries;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Application.Wrappers;

namespace SistemaGastos.Controllers;

[Authorize]
public class TransactionController(IMediator mediator, ICurrentUserService currentUserService) : Controller
{
    // GET: /Transaction
    [HttpGet]
    [Route("")]
    [Route("Index")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        var userID = currentUserService.UserId ?? 0;
        if (userID == 0) return Unauthorized("Usuario no autenticado");

        var formData = await mediator.Send(new GetTransactionFormQuery(userID, null));

        return View(formData);
    }

    [HttpGet]
    [Route("Statistics")]
    public IActionResult Statistics()
    {
        return View();
    }

    [HttpGet]
    [Route("GetList")]
    public async Task<ActionResult<Response<TransactionListDto>>> GetList(
    DateTime? startDate,
    DateTime? endDate,
    int? accountId,
    int? categoryId)
    {
        var userID = currentUserService.UserId ?? 0;
        var query = new GetTransactionListQuery(userID, startDate, endDate, accountId, categoryId);
        var result = await mediator.Send(query);
        return Ok(Response<TransactionListDto>.Ok(result));
    }

    [HttpGet]
    public async Task<IActionResult> GetCreateForm()
    {
        var userID = currentUserService.UserId ?? 0;
        if (userID == 0) return Unauthorized("Usuario no autenticado");

        var formData = await mediator.Send(new GetTransactionFormQuery(userID, null));

        return PartialView("_transactionform", formData);
    }

    [HttpGet]
    public async Task<IActionResult> GetEditForm(int id)
    {
        var userID = currentUserService.UserId ?? 0;
        if (userID == 0) return Unauthorized("Usuario no autenticado");

        var formData = await mediator.Send(new GetTransactionFormQuery(userID, id));

        if (formData == null) return NotFound("Transacción no encontrada");

        return PartialView("_EditTransactionForm", formData);
    }

    [HttpGet]
    public async Task<IActionResult> GetDropdownData()
    {
        var userID = currentUserService.UserId ?? 0;
        if (userID == 0) return Unauthorized();

        var formData = await mediator.Send(new GetTransactionFormQuery(userID, null));

        return Json(new { accounts = formData.Accounts, categories = formData.Categories });
    }

    [HttpPost]
    [Route("Save")]
    public async Task<ActionResult<Response<object>>> Save([FromBody] SaveTransactionRequest request)
    {
        var userID = currentUserService.UserId ?? 0;

        if (userID == 0)
            return Unauthorized(Response<object>.Fail("Usuario no autenticado"));

        if (request.ID > 0)
        {
            var updateCommand = new UpdateTransactionCommand
            {
                ID = request.ID,
                UserID = userID,
                Date = request.Date,
                Amount = request.Amount,
                AccountID = request.AccountID,
                CategoryID = request.CategoryID,
                Description = request.Description,
                FixedExpenseID = request.FixedExpenseID,
                PersonID = request.PersonID,
                Splits = request.Splits
            };

            var success = await mediator.Send(updateCommand);

            if (success)
                return Ok(Response<object>.Ok(new { id = request.ID }, "Transacción actualizada correctamente"));

            return Ok(Response<object>.Fail("No se pudo actualizar la transacción"));
        }

        var createCommand = new CreateTransactionCommand
        {
            UserID = userID,
            Date = request.Date,
            Amount = request.Amount,
            AccountID = request.AccountID,
            CategoryID = request.CategoryID,
            Description = request.Description,
            FixedExpenseID = request.FixedExpenseID,
            PersonID = request.PersonID,
            Splits = request.Splits
        };

        var id = await mediator.Send(createCommand);

        if (id > 0)
            return Ok(Response<object>.Ok(new { id }, "Transacción creada correctamente"));

        return Ok(Response<object>.Fail("No se pudo crear la transacción"));
    }

    [HttpPost]
    [Route("CreateBulk")]
    public async Task<ActionResult<Response<object>>> CreateBulk([FromBody] List<CreateTransactionRequest> transactions)
    {
        var userID = currentUserService.UserId ?? 0;

        if (userID == 0)
            return Unauthorized(Response<object>.Fail("Usuario no autenticado"));

        if (transactions == null || !transactions.Any())
            return BadRequest(Response<object>.Fail("No hay transacciones para crear"));

        int created = 0;
        var errors = new List<string>();

        try
        {
            foreach (var req in transactions)
            {
                try
                {
                    var command = new CreateTransactionCommand
                    {
                        UserID = userID,
                        Date = req.Date,
                        Amount = req.Amount,
                        AccountID = req.AccountID,
                        CategoryID = req.CategoryID,
                        Description = req.Description,
                        FixedExpenseID = req.FixedExpenseID,
                        PersonID = req.PersonID,
                        Splits = req.Splits
                    };

                    var id = await mediator.Send(command);
                    if (id > 0) created++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Fila {created + 1}: {ex.Message}");
                }
            }

            if (created > 0 && errors.Count == 0)
            {
                return Ok(Response<object>.Ok(new { created }, $"{created} transacciones creadas correctamente"));
            }
            else if (created > 0 && errors.Any())
            {
                return Ok(Response<object>.Ok(new { created, errors }, $"{created} creadas, {errors.Count} fallaron"));
            }
            else
            {
                return Ok(Response<object>.Fail(errors));
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, Response<object>.Fail($"Error del servidor: {ex.Message}"));
        }
    }

    // ==========================================
    // FACTURACIÓN ARCA
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> GetInvoicePreview(int id)
    {
        var userID = currentUserService.UserId ?? 0;
        if (userID == 0) return Unauthorized();

        var preview = await mediator.Send(new GetInvoicePreviewQuery(id, userID));
        if (preview is null) return NotFound("Transacción no encontrada");

        return PartialView("_InvoicePreview", preview);
    }

    [HttpPost]
    [Route("EmitInvoice")]
    public async Task<ActionResult<EmitInvoiceResultDto>> EmitInvoice([FromBody] EmitInvoiceDto dto)
    {
        var userID = currentUserService.UserId ?? 0;
        if (userID == 0) return Unauthorized();

        var result = await mediator.Send(new EmitInvoiceCommand(dto, userID));
        return Ok(result);
    }

    [HttpDelete]
    public async Task<ActionResult<Response<bool>>> Delete(int id)
    {
        var userID = currentUserService.UserId ?? 0;
        var command = new DeleteTransactionCommand(id, userID);
        var success = await mediator.Send(command);

        if (success)
            return Ok(Response<bool>.Ok(true, "Transacción eliminada correctamente"));

        return Ok(Response<bool>.Fail("No se pudo eliminar la transacción"));
    }

    [HttpGet]
    [Route("GetStatistics")]
    public async Task<ActionResult<Response<StatisticsDto>>> GetStatistics()
    {
        var userID = currentUserService.UserId ?? 0;
        var statistics = await mediator.Send(new GetStatisticsQuery(userID));
        return Ok(Response<StatisticsDto>.Ok(statistics));
    }
}

// DTOs
public class SaveTransactionRequest
{
    public int ID { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public int AccountID { get; set; }
    public int CategoryID { get; set; }
    public string Description { get; set; }
    public int? FixedExpenseID { get; set; }
    public int? PersonID { get; set; }
    public List<TransactionSplitDto> Splits { get; set; } = new();
}

public class CreateTransactionRequest
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public int AccountID { get; set; }
    public int CategoryID { get; set; }
    public string Description { get; set; }
    public int? FixedExpenseID { get; set; }
    public int? PersonID { get; set; }
    public List<TransactionSplitDto> Splits { get; set; } = new List<TransactionSplitDto>();
}
