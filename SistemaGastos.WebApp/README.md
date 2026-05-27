# SistemaGastos - Personal Finance Manager

![.NET Core](https://img.shields.io/badge/.NET%20Core-8.0-purple)
![Architecture](https://img.shields.io/badge/Architecture-Clean%20%26%20CQRS-blue)
![Status](https://img.shields.io/badge/Status-Active%20Refactoring-green)

**SistemaGastos** es una aplicación web robusta para la gestión de finanzas personales, desarrollada con **ASP.NET Core 8**. El proyecto se encuentra actualmente en un proceso de **refactorización profunda**, migrando de una arquitectura monolítica MVC tradicional hacia una **Clean Architecture** moderna implementando el patrón **CQRS**.

---

## Características Principales

### Dashboard Interactivo
- **KPIs en Tiempo Real:** Cálculo dinámico de Ingresos, Gastos, Balance y Variaciones porcentuales vs. periodos anteriores.
- **Visualización de Datos:** Gráficos interactivos (Chart.js) para distribución de gastos y evolución histórica.
- **Filtrado Avanzado:** Análisis por rangos de fecha, cuentas y categorías.

### Gestión de Transacciones (Módulo Refactorizado)
- **Grid de Alto Rendimiento:** Implementación de **Grid.js** con paginación y ordenamiento.
- **Carga Masiva (Bulk Insert):** Capacidad para registrar múltiples movimientos en una sola operación transaccional.
- **Lógica de Negocio Segura:** Actualización automática de saldos y reversión inteligente al eliminar transacciones (Rollback lógico).
- **Validaciones Estrictas:** Reglas de negocio validadas mediante **FluentValidation** antes de tocar la base de datos.

### Seguridad y Cuentas
- Gestión de Usuarios (Login/Registro).
- Aislamiento de datos por usuario (Multi-tenancy lógico).

---

## Arquitectura y Stack Tecnológico

El sistema sigue los principios de **Clean Architecture** para garantizar la escalabilidad, mantenibilidad y testabilidad del código.

### Backend (.NET 8)
- **CQRS (Command Query Responsibility Segregation):** Uso de la librería **MediatR** para desacoplar totalmente la capa de presentación (Controladores) de la lógica de negocio.
- **Entity Framework Core:** ORM para acceso a datos con SQL Server.
- **AutoMapper:** Mapeo eficiente entre Entidades de Dominio y DTOs (Data Transfer Objects) para evitar exponer el modelo de datos.
- **FluentValidation:** Validación de modelos robusta y centralizada.
- **Middleware Personalizado:** Manejo global de excepciones (`GlobalExceptionHandler`) y respuestas estandarizadas (`Response<T>`).

### Frontend
- **Razor Views:** Renderizado del lado del servidor optimizado.
- **JavaScript (ES6+):** Lógica de cliente modularizada y limpia.
- **Grid.js & Chart.js:** Componentes visuales modernos y reactivos.
- **Bootstrap 5:** Diseño responsivo y profesional.
- **SweetAlert2:** Interacciones de usuario mejoradas.

---

## ?? Estructura del Proyecto

```text
src/
??? 1. SistemaGastos.Domain      # Entidades y Reglas de Negocio (Core)
??? 2. SistemaGastos.Application # Casos de Uso (CQRS: Commands, Queries, Handlers, Validators)
??? 3. SistemaGastos.Infrastructure # Implementación de Interfaces (Data Access, External Services)
??? 4. SistemaGastos.Web         # Capa de Presentación (Controllers, Views, JS)

??? Instalación y Ejecución
1. Prerrequisitos:

.NET SDK 8.0

SQL Server

2. Clonar el repositorio:

Bash

git clone [https://github.com/tu-usuario/SistemaGastos.git](https://github.com/tu-usuario/SistemaGastos.git)

3. Configurar Base de Datos: Actualiza la cadena de conexión en appsettings.json:

JSON

"ConnectionStrings": {
  "DefaultConnection": "Server=TU_SERVIDOR;Database=SistemaGastosDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}

4. Aplicar Migraciones:

dotnet ef database update


5. Ejecutar:
dotnet run --project SistemaGastos.Web


## ??? Roadmap & Próximos Pasos
El proyecto está en constante evolución. Las siguientes mejoras están planificadas:

[ ] Dockerización: Contenerización de la API y la Base de Datos.

[ ] Testing: Implementación de Unit Tests (xUnit + Moq) para Handlers y Validadores.

[ ] Documentación API: Integración con Swagger/OpenAPI.

[ ] Optimización: Implementación de Caché (IMemoryCache) para datos estáticos (Dropdowns).

[ ] CI/CD: Pipeline de integración continua con GitHub Actions.

?? Autor
Desarrollado con pasión por [Tu Nombre]. Enfocado en Buenas Prácticas, Arquitectura de Software y Desarrollo .NET.