# FyM.Users.Web

Cliente Angular 20 (standalone, signals) para la API de usuarios y roles de FyM Technology. Tema Material 3 personalizado en azul rey.

## Estructura

```
src/app/
  core/
    api/            clientes HTTP tipados (UserApiService, RoleApiService, CatalogApiService)
    auth/            AuthService (sesión en signals), guards funcionales
    config/          ConfigService — lee /config.json en tiempo de ejecución
    http/            interceptores (auth con refresh automático, errores)
    models/          interfaces que reflejan los DTOs del backend
    notifications/   wrapper de MatSnackBar
  shared/ui/         componentes de presentación reutilizables
  layout/shell/       toolbar + sidenav que oculta ítems sin permiso
  features/           una carpeta por pantalla: auth, dashboard, users, roles, profile, errors
```

## Ejecutar sin Docker

Requisitos: Node 20+.

```bash
npm install
npm start          # ng serve, http://localhost:4200
```

Por defecto apunta a `apiBaseUrl: "/api"` (`public/config.json`); para desarrollo contra una API que corre fuera de Docker en otro puerto, edite ese archivo o configure un proxy de `ng serve` (`proxy.conf.json`) hacia `http://localhost:8080`.

## Comandos

```bash
npm start           # servidor de desarrollo
npm run build        # build de producción → dist/web/browser
npm test             # pruebas unitarias (Karma + Jasmine)
```

## Configuración en tiempo de ejecución

La URL de la API **no** se compila dentro del bundle: `ConfigService` la lee de `/config.json` al arrancar la aplicación (antes de resolver la primera ruta, vía `provideAppInitializer`). Esto permite que la misma imagen Docker sirva en cualquier entorno — el `Dockerfile` regenera ese archivo en cada arranque de contenedor a partir de la variable `API_BASE_URL`.

## Sistema de diseño

Tokens definidos como variables CSS en `src/styles.scss`, sobre el tema Material 3 (`mat.theme()`):

```scss
--fym-primary-900: #101e5a;   --fym-primary-700: #1e3a8a;   /* azul rey base */
--fym-primary-500: #2b4acb;   --fym-primary-300: #6e8af0;   --fym-primary-050: #eef2ff;
--fym-success: #10b981;       --fym-warn: #f59e0b;          --fym-danger: #e11d48;
--fym-bg: #f6f8fc;            --fym-surface: #ffffff;       --fym-border: #e2e8f0;
```

Los roles de color del sistema de Angular Material (`--mat-sys-primary`, etc.) se sobrescriben con estos mismos tonos para que todos los componentes de Material (botones, campos, tablas) hereden la paleta de marca sin tematizar cada uno manualmente.

## Cómo añadir una pantalla protegida por permiso

1. Crear el componente standalone en `features/<módulo>/`.
2. Registrar la ruta en `app.routes.ts` con `canActivate: [permissionGuard(PermissionCode.MiPermiso)]`.
3. Si debe aparecer en el menú lateral, añadirla a `NAV_ITEMS` en `layout/shell/shell.component.ts` con el mismo código de permiso — el ítem se oculta solo para quien no lo tenga.
4. Consumir el endpoint correspondiente a través de un servicio en `core/api/` (o crear uno nuevo siguiendo el mismo patrón: métodos que devuelven `Promise<T>` vía `firstValueFrom`).
