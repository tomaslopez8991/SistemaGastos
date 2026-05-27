using MediatR;
using SistemaGastos.Application.DTOs;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Application.Features.Categories.Queries;

public record GetCategoriesQuery : IRequest<List<CategoryDto>>;
public record GetCategoryByIdQuery(int Id) : IRequest<Category?>;
