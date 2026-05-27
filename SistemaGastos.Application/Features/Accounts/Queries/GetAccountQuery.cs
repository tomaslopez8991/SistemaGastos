using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.Accounts.Queries;

// Query para traer todas las cuentas de un usuario
public record GetAccountsQuery : IRequest<List<AccountDto>>;

// Query para traer una sola cuenta (para el modal de editar)
public record GetAccountByIdQuery(int Id) : IRequest<AccountDto?>;

public record GetAccountTotalsQuery : IRequest<List<AccountTotalDto>>;

public record GetTransferTargetsQuery(int OriginAccountId) : IRequest<List<AccountLookupDto>>;