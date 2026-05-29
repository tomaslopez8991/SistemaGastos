using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.Invoices.Commands;

public record EmitInvoiceCommand(EmitInvoiceDto Dto, int UserId) : IRequest<EmitInvoiceResultDto>;
