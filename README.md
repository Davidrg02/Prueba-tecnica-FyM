# Sistema de Usuarios y Roles · FyM Technology

API REST en ASP.NET Core 8 con JWT, SQL Server y Swagger, más un cliente en Angular 20, empaquetados para levantarse por completo con Docker.

## Arranque en 3 pasos

Requisito único: **Docker** (y Docker Compose, incluido en Docker Desktop).

```bash
cp .env.example .env
docker compose build
docker compose up -d
```

La primera vez tarda unos minutos (descarga de imágenes base + compilación). Verifique el estado con:

```bash
docker compose ps
```

Los tres servicios (`sqlserver`, `api`, `web`) deben quedar en `healthy`. La API aplica las migraciones y siembra los datos base (roles, permisos, catálogo de documentos y el super administrador) automáticamente al arrancar — no hay ningún paso manual adicional.

## URLs

| Servicio | URL |
|---|---|
| Aplicación web | http://localhost:4200 |
| API — Swagger | http://localhost:8080/swagger |
| API — health check | http://localhost:8080/health |
| SQL Server | `localhost,1433` (usuario `sa`) |

## Credenciales del super administrador precreado

Definidas en `.env` (`SEED_SUPERADMIN_*`); los valores de `.env.example` son:

| Campo | Valor |
|---|---|
| Correo | `admin@fymtechnology.com` |
| Contraseña | `Adm1n#FyM2026*` |

Este usuario existe desde el primer arranque, con el rol `SuperAdmin`, y es el único punto de entrada para crear el resto de usuarios (según el enunciado: la creación de usuarios nuevos es exclusiva del super administrador — en este sistema, de cualquier rol con el permiso `users.create`, que por defecto son `SuperAdmin` y `Admin`).

## Arquitectura

```
docker-compose.yml
├─ sqlserver   SQL Server 2022, con volumen persistente y healthcheck
├─ api         ASP.NET Core 8 · Clean Architecture (Domain/Application/Infrastructure/Api)
└─ web         Angular 20 standalone, servido por Nginx (proxy /api → api:8080)
```

Detalle completo en [`docs/architecture.md`](docs/architecture.md). Diagrama y racional del modelo de datos en [`docs/er-model.md`](docs/er-model.md). Estrategia de seguridad (JWT, jerarquía de roles, cifrado de la conexión) en [`docs/security.md`](docs/security.md).

## Matriz de roles

| Rol | Alcance |
|---|---|
| **SuperAdmin** | CRUD total sobre usuarios y roles, sin restricciones. |
| **Admin** | CRUD total, salvo operar sobre un usuario/rol de nivel igual o superior al suyo (en la práctica, no puede tocar al SuperAdmin) ni asignar el rol SuperAdmin. |
| **User** | Solo inicia sesión y gestiona su propio perfil. El módulo de usuarios no aparece en su menú. |

## Documentación por proyecto

- [`src/api/README.md`](src/api/README.md) — arquitectura de capas, cómo correr la API sin Docker, catálogo de endpoints, cómo crear una migración o añadir un permiso.
- [`src/web/README.md`](src/web/README.md) — estructura del frontend, comandos, sistema de diseño.

## Pruebas

```bash
# Backend (39 pruebas: unitarias de dominio/aplicación + integración end-to-end sobre SQLite en memoria)
dotnet test src/api

# Backend, dentro de un contenedor ya construido
docker compose run --rm api dotnet test

# Frontend
cd src/web && npm test
```

## Detener y limpiar

```bash
docker compose stop          # detiene los contenedores, conserva los datos
docker compose down          # elimina los contenedores, conserva el volumen de SQL Server
docker compose down -v       # elimina también los datos (reinicia desde cero)
```

## Solución de problemas

- **Puerto 1433/8080/4200 ocupado**: cambie `SQL_PORT` / `API_PORT` / `WEB_PORT` en `.env` antes de `docker compose up`.
- **SQL Server no arranca / se reinicia en bucle**: necesita al menos 2 GB de RAM asignados a Docker. Revíselo en la configuración de Docker Desktop.
- **La API tarda en quedar `healthy` la primera vez**: es normal — espera a que SQL Server esté listo, aplica migraciones y siembra los datos base. Con `docker compose logs -f api` se puede seguir el progreso.
- **Cambié el `.env` y no se aplicó**: `docker compose up -d --force-recreate` (las variables de entorno solo se releen al recrear el contenedor).

## Modelo entidad-relación

Ver [`docs/er-model.md`](docs/er-model.md) para el diagrama completo, el racional de cada decisión de diseño y el diccionario de datos. El script SQL generado desde las migraciones está en [`docs/schema.sql`](docs/schema.sql).
