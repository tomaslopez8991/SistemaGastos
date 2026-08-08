using MediatR;
using SistemaGastos.Domain.Enums;

namespace SistemaGastos.Application.Features.TmpTransactions.Commands;

public record SetCreditCardProjectionScenarioCommand(
    int UserID,
    int AccountID,
    int Year,
    int Month,
    TcProjectionMode Mode,
    decimal? CustomAmount) : IRequest<bool>;
