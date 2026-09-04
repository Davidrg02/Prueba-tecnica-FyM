import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Bloquea rutas protegidas si no hay sesión activa. */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

/** Evita que un usuario ya autenticado vuelva a ver /login o /register. */
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (typeof auth.hasExplicitLogout === 'function' && auth.hasExplicitLogout()) {
    return true;
  }

  return auth.isAuthenticated() ? router.createUrlTree(['/']) : true;
};

/** Exige, además de sesión activa, un permiso concreto (ej. "users.read"). */
export function permissionGuard(permissionCode: string): CanActivateFn {
  return (_route, state) => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.isAuthenticated()) {
      return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
    }

    return auth.hasPermission(permissionCode) ? true : router.createUrlTree(['/forbidden']);
  };
}
