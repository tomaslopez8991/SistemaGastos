# SistemaGastos

[![CI](https://github.com/tomaslopez8991/SistemaGastos1.0/actions/workflows/ci.yml/badge.svg)](https://github.com/tomaslopez8991/SistemaGastos1.0/actions/workflows/ci.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=tomaslopez8991_SistemaGastos&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=tomaslopez8991_SistemaGastos)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=tomaslopez8991_SistemaGastos&metric=bugs)](https://sonarcloud.io/summary/new_code?id=tomaslopez8991_SistemaGastos)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=tomaslopez8991_SistemaGastos&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=tomaslopez8991_SistemaGastos)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=tomaslopez8991_SistemaGastos&metric=coverage)](https://sonarcloud.io/summary/new_code?id=tomaslopez8991_SistemaGastos)

Aplicación web de gestión de finanzas personales desarrollada con **ASP.NET Core 8 MVC**, siguiendo **Clean Architecture**, **CQRS** y principios **SOLID**.

## Stack tecnológico

| Capa | Tecnología |
|---|---|
| Backend | ASP.NET Core 8, C# |
| Arquitectura | Clean Architecture + CQRS (MediatR) |
| ORM | Entity Framework Core 8 |
| Base de datos | SQL Server |
| Validaciones | FluentValidation |
| Mapeo | AutoMapper |
| Frontend | Bootstrap 5, jQuery, Chart.js, Turbo (Hotwire) |
| Contenedores | Docker + Docker Compose |
| CI/CD | GitHub Actions + SonarCloud |

## Arquitectura

```
SistemaGastos.sln
├── SistemaGastos.Domain          # Entidades, enums — sin dependencias externas
├── SistemaGastos.Application     # Casos de uso, handlers CQRS, interfaces
├── SistemaGastos.Infraestructure # EF Core, repositorios, servicios externos
└── SistemaGastos.WebApp          # Controllers MVC + Views Razor (.cshtml)
```

## Módulos

- **Dashboard** — resumen general de finanzas
- **Transacciones** — registro y gestión de movimientos
- **Mis Cuentas** — administración de cuentas bancarias/efectivo con cálculo de intereses por descubierto
- **Tarjetas de crédito** — control de gastos en tarjetas
- **Proyección** — estimaciones y proyecciones financieras
- **Estadísticas** — análisis y visualización de datos con Chart.js
- **Metas financieras** — plan y seguimiento de objetivos
- **Tareas** — gestión de tareas financieras con recordatorios
- **Categorías** — ABM de categorías de gastos/ingresos

## Correr con Docker

```bash
# Desde la raíz del repositorio
docker compose -f SistemaGastos.WebApp/docker-compose.yml up --build
```

La app queda disponible en `http://localhost:8080`.

## Correr en local

**Requisitos:** .NET 8 SDK, SQL Server

```bash
# 1. Configurar connection string en appsettings.json
# 2. Aplicar migraciones
dotnet ef database update --project SistemaGastos.Infraestructure --startup-project SistemaGastos.WebApp

# 3. Correr la app
dotnet run --project SistemaGastos.WebApp
```

## CI/CD

Cada push a `dev` o PR hacia `master` dispara el pipeline automático:

1. **Build** — compilación completa de la solución
2. **Test** — ejecución de tests con reporte de cobertura
3. **SonarCloud** — análisis estático de calidad de código
4. **Docker Build** — validación de la imagen de contenedor
