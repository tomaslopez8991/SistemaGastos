using MediatR;

namespace SistemaGastos.Application.Features.Persons.Commands;

public record DeletePersonCommand(int ID, int UserID) : IRequest<bool>;
