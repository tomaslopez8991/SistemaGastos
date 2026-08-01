using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.TmpTransactions.Queries;

public record GetDebtFlatteningAdviceQuery(int UserID, string? Question = null) : IRequest<DebtAdviceDto>;
