import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';

const PASSWORD_PATTERN = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s]).{8,}$/;

@Component({
  selector: 'fym-password-dialog',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
  ],
  template: `
    <h2 mat-dialog-title>Restablecer contraseña</h2>
    <mat-dialog-content>
      <p class="dialog-hint">Defina una contraseña temporal. El usuario deberá cambiarla al iniciar sesión.</p>
      <form [formGroup]="form" (submit)="submit($event)" novalidate>
        <mat-form-field appearance="outline">
          <mat-label>Nueva contraseña temporal</mat-label>
          <input
            matInput
            [type]="hidePassword ? 'password' : 'text'"
            formControlName="password"
            autocomplete="new-password"
          />
          <button
            mat-icon-button
            matSuffix
            type="button"
            (click)="hidePassword = !hidePassword"
            [attr.aria-label]="hidePassword ? 'Mostrar contraseña' : 'Ocultar contraseña'"
          >
            <mat-icon>{{ hidePassword ? 'visibility_off' : 'visibility' }}</mat-icon>
          </button>
          @if (form.controls.password.hasError('required') && form.controls.password.touched) {
            <mat-error>La contraseña es obligatoria.</mat-error>
          }
          @if (form.controls.password.hasError('pattern') && form.controls.password.touched) {
            <mat-error>Use mínimo 8 caracteres, mayúscula, minúscula, dígito y símbolo.</mat-error>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="dialogRef.close()">Cancelar</button>
      <button mat-flat-button color="primary" type="button" [disabled]="form.invalid" (click)="submit()">
        Restablecer contraseña
      </button>
    </mat-dialog-actions>
  `,
  styles: [
    `
      mat-form-field { width: 100%; min-width: 320px; margin-top: 8px; }
      .dialog-hint { color: var(--fym-text-muted); margin: 0; line-height: 1.45; }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PasswordDialogComponent {
  private readonly fb = inject(FormBuilder);
  readonly dialogRef = inject(MatDialogRef<PasswordDialogComponent>);
  readonly form = this.fb.nonNullable.group({
    password: ['', [Validators.required, Validators.pattern(PASSWORD_PATTERN)]],
  });
  hidePassword = true;

  submit(event?: Event): void {
    event?.preventDefault();
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.dialogRef.close(this.form.controls.password.value);
  }
}
