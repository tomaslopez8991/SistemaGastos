using MediatR;

namespace SistemaGastos.Application.Features.Persons.Commands;

public record SavePersonCommand(int UserID, int ID, string Name, int? CollectionDay, decimal? DiscountAmount, string? CollectionFrom) : IRequest<int>;
