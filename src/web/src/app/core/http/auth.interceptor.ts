import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, finalize, from, shareReplay, switchMap, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { AuthResponse } from '../models/auth.models';

/** Garantiza que, ante varias peticiones 401 simultáneas, solo se dispare
 * un único `/auth/refresh` (single-flight) y todas se reintenten con el
 * mismo token nuevo. */
@Injectable({ providedIn: 'root' })
class RefreshCoordinator {
  private refreshing$: Observable<AuthResponse> | null = null;

  refresh(auth: AuthService): Observable<AuthResponse> {
    this.refreshing$ ??= from(auth.refresh()).pipe(
      shareReplay(1),
      finalize(() => {
        this.refreshing$ = null;
      }),
    );

    return this.refreshing$;
  }
}

const AUTH_ENDPOINTS = ['/auth/login', '/auth/register', '/auth/refresh'];

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const coordinator = inject(RefreshCoordinator);

  const token = auth.accessToken();
  const authReq = req.clone({
    withCredentials: true,
    setHeaders: token ? { Authorization: `Bearer ${token}` } : {},
  });

  return next(authReq).pipe(
    catchError((error: unknown) => {
      const isAuthEndpoint = AUTH_ENDPOINTS.some((endpoint) => req.url.includes(endpoint));

      if (!(error instanceof HttpErrorResponse) || error.status !== 401 || isAuthEndpoint) {
        return throwError(() => error);
      }

      return coordinator.refresh(auth).pipe(
        switchMap((response) => {
          const retried = req.clone({
            withCredentials: true,
            setHeaders: { Authorization: `Bearer ${response.accessToken}` },
          });
          return next(retried);
        }),
        catchError((refreshError: unknown) => {
          auth.clearSession();
          void router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};
