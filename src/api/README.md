# FyM.Users.Api

API REST de usuarios y roles, en ASP.NET Core 8 con Clean Architecture. Ver [`docs/architecture.md`](../../docs/architecture.md) en la raíz del repositorio para el racional completo de la organización en capas.

## Estructura

```
src/
  FyM.Users.Domain/          entidades, políticas de negocio, excepciones — sin dependencias
  FyM.Users.Application/     casos de uso, DTOs, validadores, interfaces (Abstractions/)
  FyM.Users.Infrastructure/  EF Core, repositorios, JWT, hashing, seeding, migraciones
  FyM.Users.Api/             controladores, middleware, autorización, Program.cs
tests/
  FyM.Users.UnitTests/         Domain + Application, sin base de datos
  FyM.Users.IntegrationTests/  API completa en memoria, sobre SQLite (WebApplicationFactory)
```

**Regla de dependencias**: `Domain` ← `Application` ← `Infrastructure` ← `Api`. Cada capa solo referencia las que están por debajo.

## Ejecutar sin Docker

Requisitos: .NET SDK 8, un SQL Server accesible (local o en contenedor).

```bash
# Variables mínimas (o configúrelas en appsettings.Development.json / user-secrets)
export ConnectionStrings__Default="Server=localhost,1433;Database=FyMUsersDb;User Id=sa;Password=...;Encrypt=True;TrustServerCertificate=True;"
export Jwt__Key="una-clave-de-al-menos-32-bytes"
export Seed__SuperAdmin__Password="Adm1n#FyM2026*"

dotnet run --project src/FyM.Users.Api
```

`appsettings.Development.json` ya trae valores por defecto pensados para un SQL Server en `localhost:1433` (por ejemplo, levantado con `docker run mcr.microsoft.com/mssql/server:2022-latest`), así que en desarrollo normalmente basta con `dotnet run` sin exportar nada.

Swagger queda disponible en `https://localhost:5001/swagger` (o el puerto que asigne `dotnet run`).

## Migraciones

```bash
# Crear una migración nueva después de cambiar una entidad o un IEntityTypeConfiguration<T>
dotnet ef migrations add NombreDescriptivo \
  --project src/FyM.Users.Infrastructure \
  --startup-project src/FyM.Users.Api \
  --output-dir Persistence/Migrations

# Aplicarla a la base de datos configurada
dotnet ef database update \
  --project src/FyM.Users.Infrastructure \
  --startup-project src/FyM.Users.Api
```

En contenedor, la API aplica las migraciones pendientes automáticamente al arrancar (`Database:AutoMigrate=true`, activado por defecto — ver `Program.cs`).

## Catálogo de endpoints

Base: `/api/v1`. Documentación interactiva completa en Swagger. Resumen:

| Método | Ruta | Permiso |
|---|---|---|
| POST | `/auth/register` | anónimo |
| POST | `/auth/login` | anónimo |
| POST | `/auth/refresh` | cookie de refresh válida |
| POST | `/auth/logout` | autenticado |
| GET | `/auth/me` | autenticado |
| POST | `/auth/change-password` | autenticado |
| GET / POST | `/users` | `users.read` / `users.create` |
| GET / PUT | `/users/{id}` | `users.read` / `users.update` |
| PATCH | `/users/{id}/status` | `users.update` |
| DELETE | `/users/{id}` | `users.delete` |
| PUT | `/users/{id}/roles` | `users.assign-roles` |
| POST | `/users/{id}/reset-password` | `users.reset-password` |
| GET / PUT | `/users/me/profile` | `profile.read-own` / `profile.update-own` |
| GET / POST | `/roles` | `roles.read` / `roles.create` |
| GET / PUT / DELETE | `/roles/{id}` | `roles.read` / `roles.update` / `roles.delete` |
| PUT | `/roles/{id}/permissions` | `roles.manage-permissions` |
| GET | `/permissions` | `roles.read` |
| GET | `/catalogs/document-types` | autenticado |
| GET | `/health`, `/health/ready` | anónimo |

## Estrategia de errores

Un único `GlobalExceptionHandler` traduce cualquier excepción a `ProblemDetails`. Los controladores nunca usan `try/catch`: lanzan excepciones de dominio (`NotFoundException`, `ConflictException`, `ForbiddenOperationException`, `BusinessRuleException`) y el handler decide el código HTTP. Detalle completo en [`docs/security.md`](../../docs/security.md#manejo-de-excepciones).

## Cómo añadir un permiso nuevo

1. Agregar la constante en `FyM.Users.Domain/Enums/PermissionCode.cs`, incluyéndola en `PermissionCode.All` (el seeder la crea automáticamente en el próximo arranque).
2. Decorar el endpoint con `[HasPermission(PermissionCode.MiPermisoNuevo)]`.
3. Asignar el permiso a los roles que corresponda desde `PUT /roles/{id}/permissions` (o ajustando el seeder si debe venir precargado desde el primer arranque).

## Pruebas

```bash
dotnet test                                    # todo (unitarias + integración)
dotnet test tests/FyM.Users.UnitTests          # solo unitarias
dotnet test tests/FyM.Users.IntegrationTests   # solo integración (SQLite en memoria, sin Docker)
```
