import { Injectable, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ProblemDetails } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);

  success(message: string): void {
    this.show(message, 'fym-snack-success');
  }

  error(message: string): void {
    this.show(message, 'fym-snack-error');
  }

  info(message: string): void {
    this.show(message, 'fym-snack-info');
  }

  apiError(error: unknown, fallback: string): void {
    if (!(error instanceof HttpErrorResponse)) {
      this.error(fallback);
      return;
    }
    if (error.status === 0) {
      this.error('No se pudo conectar con el servidor. Verifique su conexión.');
      return;
    }
    const problem = error.error as ProblemDetails | undefined;
    const fieldErrors = problem?.errors
      ? Object.values(problem.errors).flat().filter(Boolean).join(' ')
      : '';
    this.error(fieldErrors || problem?.detail || problem?.title || fallback);
  }

  private show(message: string, panelClass: string): void {
    this.snackBar.open(message, 'Cerrar', {
      duration: 5000,
      horizontalPosition: 'end',
      verticalPosition: 'top',
      panelClass,
    });
  }
}
