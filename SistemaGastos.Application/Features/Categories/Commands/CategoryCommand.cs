using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.Categories.Commands;

public record SaveCategoryCommand(SaveCategoryDto Dto) : IRequest<int>;
public record DeleteCategoryCommand(int Id) : IRequest<bool>;
