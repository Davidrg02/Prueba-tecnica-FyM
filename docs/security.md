# Seguridad

## Autenticación (JWT)

- **Access token**: JWT firmado con HS256, vigencia de 15 minutos (`Jwt:AccessTokenMinutes`). Claims: `sub` (id de usuario), `jti`, `email`, `name`, `sstamp` (security stamp), `rlevel` (nivel máximo de rol), `role` (uno por rol) y `permission` (uno por permiso). Autorizar una petición nunca requiere una consulta a la base de datos: el middleware de autorización lee los claims ya validados.
- **Refresh token**: 64 bytes aleatorios (`RandomNumberGenerator`), vigencia de 7 días, entregado en una cookie `refresh_token` con `HttpOnly`, `SameSite=Lax`, `Path=/api/v1/auth` y `Secure` (configurable — ver más abajo). Solo se persiste su **hash SHA-256**; el valor en claro nunca toca la base de datos.
- **Rotación con detección de reuso**: cada `POST /auth/refresh` revoca el token presentado y emite uno nuevo, encadenado vía `ReplacedByTokenHash`. Si un token ya revocado vuelve a presentarse (señal de robo de la cookie), se revoca **toda** la familia de tokens del usuario y la petición responde `403`.
- **Invalidación inmediata**: `Users.SecurityStamp` se compara contra el claim `sstamp` en `JwtBearerEvents.OnTokenValidated`. Cambiar la contraseña, reasignar roles o desactivar la cuenta rota el stamp — los access tokens ya emitidos dejan de servir de inmediato, sin esperar su expiración natural.
- **Bloqueo por fuerza bruta**: 5 intentos fallidos consecutivos bloquean la cuenta 15 minutos (`AccessFailedCount` / `LockoutEndUtc`). El endpoint `/auth/login` además tiene un límite de tasa de 10 peticiones/minuto por IP.
- **Hashing de contraseñas**: BCrypt, factor de trabajo 12. Nunca reversible; nunca se cifra "para poder mostrarla después".

## Autorización

Policy-based, por **permiso** (no por rol): `[HasPermission(PermissionCode.UsersCreate)]` en cada endpoint, resuelto por `PermissionPolicyProvider` + `PermissionAuthorizationHandler` contra los claims `permission` del token. Añadir un permiso nuevo a un rol es un cambio de datos (`PUT /roles/{id}/permissions`), no un despliegue.

### Matriz de roles

| Rol | Alcance |
|---|---|
| **SuperAdmin** | CRUD total sobre usuarios y roles, sin restricciones de jerarquía. |
| **Admin** | CRUD total, salvo operar sobre un usuario cuyo rol tenga nivel igual o superior al suyo (en la práctica, no puede tocar a un SuperAdmin) ni asignar el rol SuperAdmin. |
| **User** | Solo autenticarse y gestionar su propio perfil. El módulo de usuarios no aparece en su menú; sus endpoints responden `403`. |

Las reglas de jerarquía (`UserAccessPolicy`, en el dominio) se aplican independientemente del rol nombrado: comparan el nivel numérico (`Roles.Level`) del actor contra el del usuario objetivo. Esto cubre, entre otras:

1. Un actor no puede operar sobre un usuario con nivel igual o superior al suyo (salvo sobre sí mismo, y salvo que el actor sea SuperAdmin).
2. Nadie puede eliminar ni desactivar su propia cuenta.
3. No se puede eliminar ni desactivar al único super administrador activo del sistema.
4. Los usuarios y roles marcados como del sistema (`IsSystem`) no se eliminan nunca.
5. Asignar el rol SuperAdmin, o cualquier rol de nivel igual o superior al del actor, requiere ser SuperAdmin.

## Manejo de excepciones

Un único `GlobalExceptionHandler` (`IExceptionHandler`, .NET 8) traduce cada excepción a `ProblemDetails` (RFC 7807):

| Excepción | Código HTTP |
|---|---|
| `FluentValidation.ValidationException` | 400, con `errors: { campo: [mensajes] }` |
| `NotFoundException` | 404 |
| `ConflictException` / `DbUpdateConcurrencyException` | 409 |
| `ForbiddenOperationException` | 403 |
| `BusinessRuleException` | 422 |
| Cualquier otra | 500, sin exponer mensaje ni stack trace fuera de `Development` |

Todas las respuestas de error incluyen `traceId` para correlacionar con los logs (Serilog, enriquecido con un `X-Correlation-Id` por petición).

## Conexión a la base de datos cifrada

Dos modos, seleccionables por configuración:

1. **Por defecto (funciona sin pasos adicionales).** Cadena de conexión con `Encrypt=True;TrustServerCertificate=True`. El tráfico va cifrado con el certificado autofirmado que SQL Server genera por sí mismo al arrancar; el cliente no valida la cadena de confianza del certificado, pero el canal sí está cifrado — `Microsoft.Data.SqlClient` 5.x ya trae `Encrypt=true` como valor por defecto. Es el modo que usa `docker-compose.yml` de fábrica.
2. **Endurecido (opcional, con certificado propio).** Para un entorno donde además se quiera validar la identidad del servidor:
   1. Generar una CA y un certificado para el CN `sqlserver` con `infra/certs/generate-certs.sh` (a crear siguiendo el patrón estándar de `openssl req`).
   2. Montar los certificados en el contenedor de SQL Server y aplicar `infra/sqlserver/mssql.conf`, que fija `network.forceencryption = 1`, `network.tlsprotocols = 1.2` y las rutas a `network.tlscert` / `network.tlskey`.
   3. Confiar en la CA desde el contenedor de la API (`update-ca-certificates`) y cambiar la cadena de conexión a `Encrypt=True;TrustServerCertificate=False;HostNameInCertificate=sqlserver`.

Este proyecto entrega el modo 1 funcionando de fábrica y documenta el modo 2 como vía de endurecimiento, dado que depende de generar y distribuir material criptográfico propio del entorno de despliegue.

## Otras medidas

- Cabeceras de seguridad (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`) en las respuestas de la API y del contenido estático servido por Nginx.
- CORS explícito por origen (`Cors:AllowedOrigins`), con `AllowCredentials` habilitado — imprescindible porque el refresh token viaja en cookie.
- Ningún secreto vive en el repositorio: `.env` está en `.gitignore`; `.env.example` trae únicamente valores de ejemplo evidentes. La clave de firma JWT (`Jwt:Key`) se valida al arrancar — la aplicación falla rápido si falta o mide menos de 32 bytes.
- El registro de auditoría (`AuditLogs`) nunca serializa `PasswordHash`, `SecurityStamp` ni `RowVersion`, ni siquiera hasheados.
