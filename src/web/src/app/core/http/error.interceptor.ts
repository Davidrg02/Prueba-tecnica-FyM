import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ProblemDetails } from '../models/api.models';
import { NotificationService } from '../notifications/notification.service';

/** Traduce cualquier error de la API (ProblemDetails RFC 7807) a un
 * snackbar en español. Los 401 se dejan pasar sin notificar: el
 * `authInterceptor` ya intentó un refresh silencioso y, si de verdad falló,
 * el guard de rutas redirige a /login sin necesidad de un toast adicional. */
export const errorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((error: unknown) => {
      const isAuthForm = ['/auth/login', '/auth/register'].some((endpoint) => req.url.includes(endpoint));
      if (error instanceof HttpErrorResponse && error.status !== 401 && !isAuthForm) {
        const notifications = inject(NotificationService);

        if (error.status === 0) {
          notifications.error('No se pudo conectar con el servidor. Verifique su conexión.');
        } else {
          const problem = error.error as ProblemDetails | undefined;
          const fieldErrors = problem?.errors
            ? Object.values(problem.errors).flat().filter(Boolean).join(' ')
            : '';
          notifications.error(fieldErrors || problem?.detail || problem?.title || 'Ocurrió un error inesperado.');
        }
      }

      return throwError(() => error);
    }),
  );
