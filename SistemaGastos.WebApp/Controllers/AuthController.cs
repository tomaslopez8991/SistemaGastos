using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SistemaGastos.Application.Features.Auth.Commands;
using SistemaGastos.Application.Features.Auth.Queries;
using SistemaGastos.Application.Interfaces;
using System.Security.Claims;

namespace SistemaGastos.WebApp.Controllers;

public class AuthController(
    IMediator mediator,
    ITurnstileService turnstileService,
    IConfiguration configuration) : Controller
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
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Login(string username, string password)
    {
        var usuario = await mediator.Send(new LoginUserQuery(username, password));

        if (usuario != null && !usuario.EmailConfirmed)
        {
            Response.StatusCode = 422;
            ViewBag.ToastType = "warning";
            ViewBag.ToastMessage = "Confirmá tu correo electrónico antes de iniciar sesión.";
            return View();
        }

        if (usuario != null && usuario.Active)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, usuario.Username),
                new("Id", usuario.ID.ToString())
            };

            if (!string.IsNullOrWhiteSpace(usuario.Email))
                claims.Add(new Claim(ClaimTypes.Email, usuario.Email));

            if (!string.IsNullOrWhiteSpace(usuario.Role))
                claims.Add(new Claim(ClaimTypes.Role, usuario.Role));

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = $"¡Bienvenido de nuevo, {usuario.Username}!";
            TempData["ShowLoginNotifications"] = true;

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
        ViewBag.TurnstileEnabled = configuration.GetValue<bool>("BotProtection:Turnstile:Enabled");
        ViewBag.TurnstileSiteKey = configuration["BotProtection:Turnstile:SiteKey"];
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("registration")]
    public async Task<IActionResult> Register(RegisterUserCommand command, string? turnstileToken)
    {
        try
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "Datos inválidos" });

            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (!await turnstileService.ValidateAsync(turnstileToken, remoteIp, HttpContext.RequestAborted))
                return Json(new { success = false, message = "No pudimos validar la solicitud. Intentá nuevamente." });

            var originUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            await mediator.Send(command with { OriginUrl = originUrl });
            return Json(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch
        {
            Response.StatusCode = 500;
            return Json(new { success = false, message = "No pudimos completar el registro. Intentá nuevamente." });
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string? token)
    {
        var confirmed = !string.IsNullOrWhiteSpace(token) &&
                        await mediator.Send(new ConfirmEmailCommand(token));

        TempData["ToastType"] = confirmed ? "success" : "error";
        TempData["ToastMessage"] = confirmed
            ? "Tu correo fue confirmado. Ya podés iniciar sesión."
            : "El enlace de confirmación no es válido o ya venció.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResendConfirmation() => View();

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("confirmation-email")]
    public async Task<IActionResult> ResendConfirmation(string email)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            var originUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            await mediator.Send(new ResendConfirmationEmailCommand(email, originUrl));
        }

        ViewBag.SuccessMessage = "Si el correo corresponde a una cuenta pendiente, enviamos un nuevo enlace de confirmación.";
        return View();
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
