using MediatR;
using SistemaGastos.Application.DTOs;

namespace SistemaGastos.Application.Features.Notifications.Queries;

public record GetAllNotificationsQuery(int UserId) : IRequest<List<NotificationDto>>;
