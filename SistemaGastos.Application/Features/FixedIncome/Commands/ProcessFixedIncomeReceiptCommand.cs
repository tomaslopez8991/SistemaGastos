using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.FixedIncome.Commands;

public record ProcessFixedIncomeReceiptCommand(int FixedIncomeID, int UserID) : IRequest<ReceiptResultDto>;
