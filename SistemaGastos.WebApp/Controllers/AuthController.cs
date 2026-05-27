using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGastos.Application.Features.Auth.Commands;
using SistemaGastos.Application.Features.Auth.Queries;
using System.Security.Claims;

namespace SistemaGastos.WebApp.Controllers;

public class AuthController(IMediator mediator) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        if (User.Identity!.IsAuthenticated) return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string username, string password)
    {
        var usuario = await mediator.Send(new LoginUserQuery(username, password));

        if (usuario != null && usuario.Active)
        {
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, usuario.Username),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.Role),
            new Claim("Id", usuario.ID.ToString())
        };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = $"¡Bienvenido de nuevo, {usuario.Username}!";

            return RedirectToAction("Index", "Home");
        }

        Response.StatusCode = 422;

        ViewBag.ToastType = "error";
        ViewBag.ToastMessage = "Usuario o contraseña incorrectos.";

        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity!.IsAuthenticated) return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterUserCommand command)
    {
        try
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "Datos inválidos" });

            await mediator.Send(command);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // --- FORGOT PASSWORD ---

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        var originUrl = $"{Request.Scheme}://{Request.Host}";
        await mediator.Send(new ForgotPasswordCommand(email, originUrl));

        ViewData["successLogin"] = "Si el correo existe, enviamos las instrucciones.";
        return View();
    }

    // --- LOGOUT ---

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        TempData.Clear();

        TempData["ToastType"] = "info";
        TempData["ToastMessage"] = "Sesión cerrada correctamente.";

        return RedirectToAction("Login");
    }
}