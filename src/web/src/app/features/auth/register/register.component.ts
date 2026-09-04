import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { AuthService } from '../../../core/auth/auth.service';
import { ProblemDetails } from '../../../core/models/api.models';
import { DocumentType } from '../../../core/models/role.models';

/** Misma política que FyM.Users.Application.Common.PasswordPolicy en el backend. */
const PASSWORD_PATTERN = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s]).{8,}$/;

@Component({
  selector: 'fym-register',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatSelectModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly hidePassword = signal(true);
  readonly documentTypes = signal<DocumentType[]>([]);

  readonly form = this.fb.nonNullable.group({
    userName: ['', [Validators.required, Validators.maxLength(50)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.pattern(PASSWORD_PATTERN)]],
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    documentTypeId: this.fb.control<number | null>(null),
    documentNumber: [''],
    phoneNumber: [''],
  });

  readonly passwordChecks = computed(() => {
    const value = this.form.controls.password.value;
    return {
      length: value.length >= 8,
      lower: /[a-z]/.test(value),
      upper: /[A-Z]/.test(value),
      digit: /\d/.test(value),
      symbol: /[^\w\s]/.test(value),
    };
  });

  async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    try {
      const raw = this.form.getRawValue();
      await this.auth.register({
        ...raw,
        documentNumber: raw.documentNumber || null,
        phoneNumber: raw.phoneNumber || null,
      });
      await this.router.navigateByUrl('/');
    } catch (error) {
      const problem = error instanceof HttpErrorResponse ? (error.error as ProblemDetails | undefined) : undefined;
      this.errorMessage.set(problem?.detail ?? 'No se pudo completar el registro. Intente nuevamente.');
    } finally {
      this.loading.set(false);
    }
  }
}
