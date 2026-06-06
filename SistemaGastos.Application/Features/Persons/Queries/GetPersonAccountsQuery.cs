using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.Persons.Queries;

/// <param name="Year">Mes de referencia para el cálculo de cuotas (default = mes actual).</param>
/// <param name="Month">Mes de referencia para el cálculo de cuotas (default = mes actual).</param>
public record GetPersonAccountsQuery(int UserID, int Year = 0, int Month = 0) : IRequest<List<PersonAccountDto>>;
