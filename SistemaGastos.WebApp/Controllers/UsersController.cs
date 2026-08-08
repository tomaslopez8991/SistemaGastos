using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Features.Users.Commands;
using SistemaGastos.Application.Features.Users.Queries;
using SistemaGastos.WebApp.Services;

namespace SistemaGastos.Controllers;

[Authorize]
public class UsersController(IMediator mediator, ICurrentUserService currentUser) : Controller
{
    // GET: Mi Perfil
    public async Task<IActionResult> Profile()
    {
        var username = currentUser.Username;
        var user = await mediator.Send(new GetUserProfileQuery(username));

        if (user == null) return RedirectToAction("Logout", "Auth");

        var model = new UserVM
        {
            ID = user.ID,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role
        };

        return View(model);
    }

    // POST: Actualizar Info
    [HttpPost]
    public async Task<IActionResult> UpdateInfo([FromBody] UserVM data)
    {
        var resultado = await mediator.Send(new UpdateUserProfileCommand(data.ID, data.Email));
        return Json(new { success = resultado });
    }

    // POST: Cambiar Password
    [HttpPost]
    public async Task<IActionResult> UpdatePassword([FromBody] ChangePasswordVM data)
    {
        var resultado = await mediator.Send(new ChangeUserPasswordCommand(data.UserID, data.NewPassword));
        return Json(new { success = resultado });
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index()
    {
        var users = await mediator.Send(new GetAllUsersQuery(currentUser.Username));
        return View(users);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetStatus(int id, bool active)
    {
        if (id == currentUser.UserId) return BadRequest(new { success = false, message = "No podés modificar tu propia cuenta." });
        var result = await mediator.Send(new ToggleUserStatusCommand(id, active));
        return result
            ? Json(new { success = true })
            : BadRequest(new { success = false, message = active ? "El usuario debe confirmar su correo antes de activarse." : "No se pudo actualizar el usuario." });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetRole(int id, string role)
    {
        if (id == currentUser.UserId) return BadRequest(new { success = false, message = "No podés modificar tu propio rol." });
        var result = await mediator.Send(new SetUserRoleCommand(id, role));
        return result
            ? Json(new { success = true })
            : BadRequest(new { success = false, message = "No se pudo actualizar el rol." });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetDeleted(int id, bool deleted)
    {
        if (id == currentUser.UserId) return BadRequest(new { success = false, message = "No podés dar de baja tu propia cuenta." });
        var result = await mediator.Send(new SetUserDeletedCommand(id, deleted));
        return result
            ? Json(new { success = true })
            : BadRequest(new { success = false, message = "No se pudo actualizar el usuario." });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ResendConfirmation(int id)
    {
        var originUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        var result = await mediator.Send(new ResendUserConfirmationCommand(id, originUrl));
        return result
            ? Json(new { success = true })
            : BadRequest(new { success = false, message = "El usuario ya está confirmado o no está disponible." });
    }

    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity!.IsAuthenticated) return RedirectToAction("Register", "Login");
        return View();
    }
}
