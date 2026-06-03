using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.Persons.Queries;

public record GetPersonAccountsQuery(int UserID) : IRequest<List<PersonAccountDto>>;
