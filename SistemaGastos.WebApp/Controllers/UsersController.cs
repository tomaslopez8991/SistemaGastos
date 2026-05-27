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

    // GET: Listado Admin
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index() // Vista: Views/Users/Index.cshtml
    {
        var users = await mediator.Send(new GetAllUsersQuery(currentUser.Username));
        return View(users);
    }

    // POST: Activar Usuario
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetActive(int idUser)
    {
        var result = await mediator.Send(new ToggleUserStatusCommand(idUser, true));
        if (!result) return NotFound();
        return Json(new { success = true });
    }

    // POST: Desactivar Usuario
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetInactive(int idUser)
    {
        var result = await mediator.Send(new ToggleUserStatusCommand(idUser, false));
        if (!result) return NotFound();
        return Json(new { success = true });
    }

    // POST: Eliminar Usuario
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? ID)
    {
        if (ID == null || ID == 0) return NotFound();

        var result = await mediator.Send(new DeleteUserCommand(ID.Value));
        if (!result) return NotFound();

        return Json(new { success = true });
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
