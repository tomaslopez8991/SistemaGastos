using FluentValidation;
using MediatR;
using StackExchange.Profiling;
using Microsoft.AspNetCore.Authentication.Cookies; // NECESARIO PARA AUTH
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides; // NECESARIO PARA SOMEE
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaGastos;
using SistemaGastos.Application.Behaviors;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Application.Features.Transactions.Validators;
using SistemaGastos.Data;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Application.Options;
using SistemaGastos.Infraestructure.Services;
using SistemaGastos.WebApp.Services;
using System;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Shared hosting accounts cannot write to the Windows Event Log.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// 1. CONFIGURACI�N DE BASE DE DATOS
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, b => b.MigrationsAssembly("SistemaGastos.Infraestructure")));

// 2. PERSISTENCIA DE LLAVES (DATA PROTECTION) - CR�TICO EN SOMEE
// Use ContentRootPath directly (not nested wwwroot) so the keys folder is writable on shared hosting
var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "keys");
var dataProtection = builder.Services.AddDataProtection()
    .SetApplicationName("SistemaGastosApp");

try
{
    Directory.CreateDirectory(keysFolder);

    var probePath = Path.Combine(keysFolder, $".write-test-{Guid.NewGuid():N}");
    File.WriteAllText(probePath, string.Empty);
    File.Delete(probePath);

    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysFolder));
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Data Protection keys could not be persisted: {ex.Message}");
    dataProtection.UseEphemeralDataProtectionProvider();
}

// 3. CONFIGURACI�N DE COOKIES (POL�TICA LAXA PARA QUE FUNCIONE SIEMPRE)
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => false;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
});

// 4. AGREGAR SERVICIO DE AUTENTICACI�N (FALTABA ESTO)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.Events.OnRedirectToLogin = context => {
            context.Response.Redirect("/Auth/Login");
            return Task.CompletedTask;
        };
        options.Events.OnValidatePrincipal = async context =>
        {
            var idValue = context.Principal?.FindFirst("Id")?.Value;
            if (!int.TryParse(idValue, out var userId))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var user = await db.Login
                .AsNoTracking()
                .Where(x => x.ID == userId)
                .Select(x => new { x.Active, x.IsDeleted, x.Role })
                .FirstOrDefaultAsync(context.HttpContext.RequestAborted);
            var currentRole = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (user == null || !user.Active || user.IsDeleted || user.Role != currentRole)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

// 5. CONFIGURACI�N DE SESI�N
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    // IMPORTANTE: 'None' es lo m�s compatible para evitar problemas con el Proxy de Somee
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddSignalR();
builder.Services.AddHttpClient<IDolarService, DolarService>();
builder.Services.AddHttpClient<ITurnstileService, TurnstileService>();
builder.Services.AddScoped<DolarService>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("registration", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(30),
                QueueLimit = 0
            }));
    options.AddPolicy("confirmation-email", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0
            }));
});

builder.Services.AddValidatorsFromAssemblyContaining<CreateTransactionValidator>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

builder.Services.AddScoped<IAccountInterestService, SistemaGastos.Infraestructure.Services.AccountInterestService>();

builder.Services.AddHttpClient<SistemaGastos.Infraestructure.Services.ClaudeService>();
builder.Services.AddHttpClient<SistemaGastos.Infraestructure.Services.OllamaService>();
builder.Services.AddScoped<IAiService>(provider =>
{
    var defaultProvider = builder.Configuration["AiProviders:Default"] ?? "ollama";
    return defaultProvider switch
    {
        "claude" => provider.GetRequiredService<SistemaGastos.Infraestructure.Services.ClaudeService>(),
        _ => provider.GetRequiredService<SistemaGastos.Infraestructure.Services.OllamaService>()
    };
});

builder.Services.AddScoped<IEmailTemplateHelper, EmailTemplateHelper>();
builder.Services.AddTransient<IARCAService, ARCAServiceStub>();
builder.Services.Configure<FiscalConfigOptions>(builder.Configuration.GetSection("FiscalConfig"));

//AUTOMAPPING DE ENTIDADES
builder.Services.AddAutoMapper(typeof(SistemaGastos.Application.Mappings.MappingProfile));

// Esto escanea todo tu proyecto Application buscando Handlers y los registra
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SistemaGastos.Application.Mappings.MappingProfile).Assembly));

builder.Services.AddTransient<SistemaGastos.WebApp.Middleware.GlobalExceptionHandlerMiddleware>();

// MiniProfiler — visible solo para el usuario configurado en MiniProfiler:AdminUsername
var profilerAdmin = builder.Configuration["MiniProfiler:AdminUsername"];
if (!string.IsNullOrEmpty(profilerAdmin))
{
    builder.Services.AddMiniProfiler(options =>
    {
        options.RouteBasePath = "/profiler";
        options.ColorScheme = ColorScheme.Dark;
        options.PopupRenderPosition = RenderPosition.BottomLeft;
        options.PopupShowTimeWithChildren = true;
        options.ResultsAuthorize = req =>
            req.HttpContext.User?.Identity?.IsAuthenticated == true &&
            req.HttpContext.User.Identity.Name == profilerAdmin;
        options.ResultsListAuthorize = req =>
            req.HttpContext.User?.Identity?.IsAuthenticated == true &&
            req.HttpContext.User.Identity.Name == profilerAdmin;
    }).AddEntityFramework();
}

builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(SistemaGastos.Application.Features.Accounts.Queries.GetAccountsQuery).Assembly);

    // 2. REGISTRAR EL PIPELINE (IMPORTANTE)
    // Esto le dice a MediatR: "Usa este comportamiento para validar"
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// 3. Registrar Validadores autom�ticamente
// Escanea todo el proyecto Application buscando clases que hereden de AbstractValidator
builder.Services.AddValidatorsFromAssembly(typeof(SistemaGastos.Application.Features.Accounts.Validators.CreateAccountCommandValidator).Assembly);

var app = builder.Build();

// AUTO-MIGRACIÓN: aplica migraciones pendientes al iniciar la app
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    // Actualiza el log diario de intereses y genera el cobro mensual si corresponde
    // Wrapped in try-catch: a failure here must not prevent the app from starting
    try
    {
        var accountInterestService = scope.ServiceProvider.GetRequiredService<IAccountInterestService>();
        await accountInterestService.RunAccrualAsync();
    }
    catch (Exception ex)
    {
        var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        startupLogger.LogError(ex, "RunAccrualAsync failed on startup — app will continue");
    }
}

// 6. CONFIGURACI�N DE PROXY (CR�TICO PARA HTTPS EN SOMEE)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Configuraci�n Regional
var defaultCulture = "es-AR";
var ci = new CultureInfo(defaultCulture);
ci.NumberFormat.NumberDecimalSeparator = ".";
ci.NumberFormat.CurrencyDecimalSeparator = ",";

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(ci),
    SupportedCultures = new List<CultureInfo> { ci },
    SupportedUICultures = new List<CultureInfo> { ci }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCookiePolicy();

app.UseMiddleware<SistemaGastos.WebApp.Middleware.GlobalExceptionHandlerMiddleware>();

app.UseRouting();
app.UseRateLimiter();

if (!string.IsNullOrEmpty(builder.Configuration["MiniProfiler:AdminUsername"]))
    app.UseMiniProfiler();

// 7. EL ORDEN IMPORTA (AQU� ESTABA EL ERROR PRINCIPAL)
// Primero Sesi�n -> Luego Autenticaci�n -> Al final Autorizaci�n

app.UseSession();          // <--- 1. Cargar Sesi�n
app.UseAuthentication();   // <--- 2. Descifrar Qui�n soy (FALTABA)
app.UseAuthorization();    // <--- 3. Verificar Permisos

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.MapGet("/health", async (ApplicationDbContext db, CancellationToken cancellationToken) =>
{
    try
    {
        if (!await db.Database.CanConnectAsync(cancellationToken))
        {
            return Results.Json(new { status = "unhealthy", database = "unavailable" }, statusCode: 503);
        }

        var pendingMigrations = await db.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pendingMigrations.Any())
        {
            return Results.Json(new
            {
                status = "unhealthy",
                database = "connected",
                pendingMigrations = pendingMigrations.Count()
            }, statusCode: 503);
        }

        return Results.Ok(new { status = "healthy", database = "connected" });
    }
    catch
    {
        return Results.Json(new { status = "unhealthy", database = "error" }, statusCode: 503);
    }
}).AllowAnonymous();

app.Run();
