# Arquitectura

## Backend: Clean Architecture por capas

```
FyM.Users.Domain          (sin dependencias)
        ↑
FyM.Users.Application      → Domain
        ↑
FyM.Users.Infrastructure   → Application
        ↑
FyM.Users.Api              → Application + Infrastructure
```

**Regla de dependencias** (verificable con `dotnet list <proyecto> reference`): cada capa solo conoce las que están por debajo de ella en el diagrama. `Domain` no referencia ningún paquete de infraestructura (ni EF Core, ni ASP.NET Core); `Application` define las *interfaces* que `Infrastructure` implementa (patrón de inversión de dependencias), de modo que los casos de uso nunca dependen directamente de EF Core, JWT o BCrypt — solo de abstracciones (`IUserRepository`, `ITokenService`, `IPasswordHasher`, …).

### Domain

Entidades (`User`, `Role`, `Permission`, …), enums (`SystemRole`, `PermissionCode`), excepciones de dominio (`NotFoundException`, `ConflictException`, `ForbiddenOperationException`, `BusinessRuleException`) y la pieza central de las reglas de negocio: `UserAccessPolicy`, que decide quién puede operar sobre quién comparando `Roles.Level`. No tiene ninguna dependencia externa — se puede testear con `dotnet test` sin base de datos, sin HTTP, sin nada.

### Application

Casos de uso (`AuthService`, `UserService`, `RoleService`, …), DTOs de entrada/salida, validadores de FluentValidation y las interfaces que necesita para operar (`Abstractions/`). Orquesta al dominio; no sabe si los datos vienen de SQL Server o de una base en memoria.

### Infrastructure

Implementación de esas interfaces: `AppDbContext` y sus `IEntityTypeConfiguration<T>` (una por entidad, no data annotations dispersas), repositorios, interceptores de auditoría, `JwtTokenService`, `BCryptPasswordHasher` y el `DatabaseSeeder`.

### Api

Composición: controladores finos (delegan todo a los servicios de Application), middleware transversal (`GlobalExceptionHandler`, `CorrelationIdMiddleware`), autorización por permiso (`HasPermissionAttribute` + `PermissionPolicyProvider` + `PermissionAuthorizationHandler`) y el registro de DI (`Program.cs`). Es la única capa que sabe que existe HTTP, cookies o Swagger.

### Por qué no MediatR/CQRS

Se evaluó una variante con `MediatR` (comando/query + handler por caso de uso), pero para el tamaño de este dominio (dos agregados: usuarios y roles) añade una capa de indirección — un archivo `Command`, uno `Handler` y uno `Validator` por operación — sin un beneficio claro sobre servicios de aplicación directos con inyección de dependencias. Se optó por el enfoque más simple y legible.

## Frontend: Angular 20 standalone

Sin `NgModule`: todo componente es standalone, con `imports: [...]` explícitos. Enrutamiento con `loadComponent` (cada pantalla es su propio *lazy chunk*, visible en la salida de `ng build`). Estado con **signals** (`signal`, `computed`) en lugar de `BehaviorSubject` para el estado síncrono de la sesión; RxJS se reserva para lo que es genuinamente asíncrono/flujo (interceptores HTTP).

```
core/            servicios transversales: auth, config runtime, interceptores HTTP,
                 notificaciones, clientes de API tipados
shared/ui/       componentes de presentación reutilizables (page-header, empty-state,
                 status-chip, role-chips, confirm-dialog)
layout/shell/    toolbar + sidenav que oculta ítems sin permiso
features/        una carpeta por pantalla (auth, dashboard, users, roles, profile, errors)
```

**Por qué el par create/edit de usuarios y de roles comparte un único componente** (`UserFormComponent`, `RoleDetailComponent`): la diferencia entre "crear" y "editar" es, en ambos casos, la presencia de un `id` en la ruta y un par de campos adicionales (contraseña inicial, nivel del rol) — separar en dos componentes hubiese duplicado el 90% del formulario.

**Autenticación**: el access token vive únicamente en un signal en memoria (`AuthService`), nunca en `localStorage`. El refresh token es una cookie `HttpOnly` que el navegador administra solo; el frontend nunca la lee ni la escribe directamente. Al recargar la página, `provideAppInitializer` dispara `AuthService.bootstrap()`, que intenta un `POST /auth/refresh` silencioso antes de que el router resuelva la primera ruta — así una recarga no desloguea al usuario.

**Interceptores** (orden de registro importa): `errorInterceptor` envuelve a `authInterceptor`. Ante un `401`, `authInterceptor` intenta refrescar la sesión y reintentar la petición original *antes* de que `errorInterceptor` llegue a mostrar un mensaje de error — así un refresh silencioso exitoso no genera un parpadeo de error en pantalla. Varias peticiones en paralelo que reciben `401` comparten un único `POST /auth/refresh` (patrón *single-flight* vía `shareReplay`).

**Configuración en tiempo de ejecución**: la URL de la API no se compila dentro del bundle. `ConfigService` lee `/config.json` (un archivo estático, no procesado por Webpack/esbuild) al arrancar; la imagen Docker del frontend regenera ese archivo en cada arranque de contenedor a partir de la variable de entorno `API_BASE_URL` (`infra/nginx/docker-entrypoint.sh`). La misma imagen sirve en cualquier entorno sin reconstruir.

## Por qué el mismo origen para front y API (Nginx como proxy)

`docker-compose.yml` publica un único puerto para el frontend (`4200`); Nginx sirve el bundle de Angular y reenvía `/api/*` al contenedor de la API. Esto hace que, desde el navegador, el frontend y la API compartan origen — la cookie de refresh es *first-party* y no hay que lidiar con `SameSite=None` ni configuraciones de CORS más permisivas de lo necesario. El puerto de la API también se publica por separado (`8080`) únicamente para poder abrir Swagger directamente durante el desarrollo/evaluación.
