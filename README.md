# SistemaGastos

[![CI](https://github.com/tomaslopez8991/SistemaGastos/actions/workflows/ci.yml/badge.svg)](https://github.com/tomaslopez8991/SistemaGastos/actions/workflows/ci.yml)
[![CD](https://github.com/tomaslopez8991/SistemaGastos/actions/workflows/cd.yml/badge.svg)](https://github.com/tomaslopez8991/SistemaGastos/actions/workflows/cd.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=tomaslopez8991_SistemaGastos&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=tomaslopez8991_SistemaGastos)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=tomaslopez8991_SistemaGastos&metric=bugs)](https://sonarcloud.io/summary/new_code?id=tomaslopez8991_SistemaGastos)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=tomaslopez8991_SistemaGastos&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=tomaslopez8991_SistemaGastos)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=tomaslopez8991_SistemaGastos&metric=coverage)](https://sonarcloud.io/summary/new_code?id=tomaslopez8991_SistemaGastos)

Aplicación web de gestión de finanzas personales desarrollada con **ASP.NET Core 8 MVC**, siguiendo **Clean Architecture**, **CQRS** y principios **SOLID**. Deployada en **Azure App Service** con pipeline CI/CD completo.

---

## Stack tecnológico

| Capa | Tecnología |
|---|---|
| Backend | ASP.NET Core 8, C# |
| Arquitectura | Clean Architecture + CQRS (MediatR) |
| ORM | Entity Framework Core 8 |
| Base de datos | SQL Server |
| Validaciones | FluentValidation (pipeline behavior) |
| Mapeo | AutoMapper |
| Frontend | Bootstrap 5, jQuery, Chart.js |
| Contenedores | Docker + Docker Compose |
| CI/CD | GitHub Actions + SonarCloud + Azure App Service |

---

## Arquitectura

```
SistemaGastos.sln
├── SistemaGastos.Domain          # Entidades, enums — sin dependencias externas
├── SistemaGastos.Application     # Casos de uso, handlers CQRS, interfaces, DTOs
├── SistemaGastos.Infraestructure # EF Core, migraciones, servicios externos
└── SistemaGastos.WebApp          # Controllers MVC, Razor Views, Program.cs
```

Las dependencias fluyen en una sola dirección: `WebApp → Application → Domain`. Infrastructure implementa las interfaces definidas en Application (inversión de dependencias). Los controllers solo conocen `IMediator` — toda la lógica de negocio vive en los handlers.

---

## Módulos

| Módulo | Descripción |
|---|---|
| **Dashboard** | Resumen general: saldos, ingresos/egresos del mes, intereses devengados |
| **Transacciones** | Registro de movimientos con soporte de splits por cuenta |
| **Cuentas** | ABM de cuentas bancarias/efectivo, transferencias, cálculo de intereses por descubierto |
| **Tarjetas de crédito** | Control de gastos en tarjetas con cuotas |
| **Gastos fijos** | Gastos recurrentes con auditoría de cambios de precio e historial |
| **Ingresos fijos** | Ingresos recurrentes con procesamiento de cobros |
| **Presupuestos** | Definición y seguimiento de presupuestos por categoría |
| **Proyección** | Calendario cashflow y proyección de saldo a 12 meses |
| **Estadísticas** | Análisis y visualización de datos con Chart.js |
| **Personas** | Atribución de gastos/ingresos por persona con porcentaje |
| **Tareas** | Gestión de tareas financieras con prioridad y recordatorios |
| **Categorías** | ABM de categorías de gastos e ingresos |
| **Facturación** | Emisión de comprobantes vía ARCA/AFIP (stub configurable) |

---

## Correr con Docker

```bash
# Desde la raíz del repositorio
docker compose -f SistemaGastos.WebApp/docker-compose.yml up --build
```

La app queda disponible en `http://localhost:8080`.

---

## Correr en local

**Requisitos:** .NET 8 SDK, SQL Server

**1. Configurar la connection string** en `SistemaGastos.WebApp/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SistemaGastos;Trusted_Connection=True;"
  }
}
```

**2. Aplicar migraciones** (la app también las aplica automáticamente al iniciar):

```bash
dotnet ef database update --project SistemaGastos.Infraestructure --startup-project SistemaGastos.WebApp
```

**3. Correr la app:**

```bash
dotnet run --project SistemaGastos.WebApp
```

---

## Variables de entorno / configuración

| Clave | Descripción | Requerido |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | Connection string a SQL Server | ✅ |
| `Email__Host` | Servidor SMTP para envío de emails | Opcional |
| `Email__Port` | Puerto SMTP | Opcional |
| `Email__Username` | Usuario SMTP | Opcional |
| `Email__Password` | Contraseña SMTP | Opcional |
| `FiscalConfig__*` | Configuración ARCA/AFIP para facturación | Opcional |
| `AZURE_WEBAPP_NAME` | Nombre del App Service (variable de GitHub Actions) | Solo CI/CD |

---

## CI/CD

El pipeline se ejecuta automáticamente en cada push a `dev` o PR hacia `master`:

```
push/PR
  └── CI (build + test + SonarCloud + Docker build)
        └── CD (deploy a Azure App Service) ← solo en merge a master
```

1. **Build** — compilación completa de la solución
2. **Test** — ejecución de tests con reporte de cobertura OpenCover
3. **SonarCloud** — análisis estático: bugs, code smells, cobertura
4. **Docker Build** — validación de la imagen de contenedor
5. **Deploy** — publicación en Azure App Service (solo `master`)

---

## Flujo de ramas

```
master        ← producción (Azure)
  └── dev     ← integración
        └── feature/fix/refactor/* ← desarrollo
```

Todo cambio se desarrolla en una rama propia desde `dev`, se integra vía PR, y se promueve a `master` cuando está listo para producción.
