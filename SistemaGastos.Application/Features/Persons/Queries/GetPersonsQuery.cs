using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.Persons.Queries;

public record GetPersonsQuery(int UserID) : IRequest<List<PersonDto>>;
