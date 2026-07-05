using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.TmpTransactions.Queries;

public record GetDebtSnapshotQuery(int UserID) : IRequest<DebtSnapshotDto>;
