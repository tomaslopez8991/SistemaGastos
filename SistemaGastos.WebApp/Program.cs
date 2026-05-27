using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies; // NECESARIO PARA AUTH
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides; // NECESARIO PARA SOMEE
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaGastos;
using SistemaGastos.Application.Behaviors;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Application.Validators;
using SistemaGastos.Data;
using SistemaGastos.Infraestructure.Services;
using SistemaGastos.WebApp.Services;
using System;
using System.Globalization;
using System.Net;
using System.Net.Mail;

var builder = WebApplication.CreateBuilder(args);

// 1. CONFIGURACIÓN DE BASE DE DATOS
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, b => b.MigrationsAssembly("SistemaGastos.Infraestructure")));

// 2. PERSISTENCIA DE LLAVES (DATA PROTECTION) - CRÍTICO EN SOMEE
var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "keys");
if (!Directory.Exists(keysFolder)) Directory.CreateDirectory(keysFolder);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
    .SetApplicationName("SistemaGastosApp");

// 3. CONFIGURACIÓN DE COOKIES (POLÍTICA LAXA PARA QUE FUNCIONE SIEMPRE)
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => false;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
});

// 4. AGREGAR SERVICIO DE AUTENTICACIÓN (FALTABA ESTO)
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
    });

// 5. CONFIGURACIÓN DE SESIÓN
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    // IMPORTANTE: 'None' es lo más compatible para evitar problemas con el Proxy de Somee
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddSignalR();
builder.Services.AddHttpClient<IDolarService, DolarService>();
builder.Services.AddScoped<DolarService>();

builder.Services.AddValidatorsFromAssemblyContaining<CreateTransactionValidator>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

builder.Services.AddScoped<IEmailTemplateHelper, EmailTemplateHelper>();

//AUTOMAPPING DE ENTIDADES
builder.Services.AddAutoMapper(typeof(SistemaGastos.Application.Mappings.MappingProfile));

// Esto escanea todo tu proyecto Application buscando Handlers y los registra
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SistemaGastos.Application.Mappings.MappingProfile).Assembly));

builder.Services.AddTransient<SistemaGastos.WebApp.Middleware.GlobalExceptionHandlerMiddleware>();

builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(SistemaGastos.Application.Features.Accounts.Queries.GetAccountsQuery).Assembly);

    // 2. REGISTRAR EL PIPELINE (IMPORTANTE)
    // Esto le dice a MediatR: "Usa este comportamiento para validar"
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// 3. Registrar Validadores automáticamente
// Escanea todo el proyecto Application buscando clases que hereden de AbstractValidator
builder.Services.AddValidatorsFromAssembly(typeof(SistemaGastos.Application.Features.Accounts.Validators.CreateAccountCommandValidator).Assembly);

var app = builder.Build();

// 6. CONFIGURACIÓN DE PROXY (CRÍTICO PARA HTTPS EN SOMEE)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Configuración Regional
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

// 7. EL ORDEN IMPORTA (AQUÍ ESTABA EL ERROR PRINCIPAL)
// Primero Sesión -> Luego Autenticación -> Al final Autorización

app.UseSession();          // <--- 1. Cargar Sesión
app.UseAuthentication();   // <--- 2. Descifrar Quién soy (FALTABA)
app.UseAuthorization();    // <--- 3. Verificar Permisos

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();