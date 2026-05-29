using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.Invoices.Queries;

public record GetInvoicePreviewQuery(int TransactionId, int UserId) : IRequest<InvoicePreviewDto?>;
