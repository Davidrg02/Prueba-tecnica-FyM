import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { UserApiService } from '../../core/api/user-api.service';
import { AuthService } from '../../core/auth/auth.service';
import { ProblemDetails } from '../../core/models/api.models';
import { UserDetail } from '../../core/models/user.models';
import { NotificationService } from '../../core/notifications/notification.service';
import { PageHeaderComponent } from '../../shared/ui/page-header/page-header.component';

const PASSWORD_PATTERN = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s]).{8,}$/;

@Component({
  selector: 'fym-profile',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    PageHeaderComponent,
  ],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfileComponent {
  private readonly fb = inject(FormBuilder);
  private readonly userApi = inject(UserApiService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);

  readonly loading = signal(true);
  readonly savingProfile = signal(false);
  readonly savingPassword = signal(false);
  readonly profile = signal<UserDetail | null>(null);
  readonly currentUser = this.auth.currentUser;

  readonly hideCurrentPassword = signal(true);
  readonly hideNewPassword = signal(true);

  readonly profileForm = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    middleName: [''],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    secondLastName: [''],
    phoneNumber: [''],
  });

  readonly passwordForm = this.fb.nonNullable.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, Validators.pattern(PASSWORD_PATTERN)]],
  });

  constructor() {
    void this.load();
  }

  private async load(): Promise<void> {
    try {
      const profile = await this.userApi.getOwnProfile();
      this.profile.set(profile);
      this.profileForm.patchValue({
        firstName: profile.firstName,
        middleName: profile.middleName ?? '',
        lastName: profile.lastName,
        secondLastName: profile.secondLastName ?? '',
        phoneNumber: profile.phoneNumber ?? '',
      });
    } catch (error) {
      this.notifications.apiError(error, 'No se pudo cargar el perfil.');
    } finally {
      this.loading.set(false);
    }
  }

  async saveProfile(): Promise<void> {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.savingProfile.set(true);
    try {
      const updated = await this.userApi.updateOwnProfile(this.profileForm.getRawValue());
      this.profile.set(updated);
      this.notifications.success('Perfil actualizado correctamente.');
    } catch (error) {
      const problem = error instanceof HttpErrorResponse ? (error.error as ProblemDetails | undefined) : undefined;
      this.notifications.apiError(error, 'No se pudo actualizar el perfil.');
    } finally {
      this.savingProfile.set(false);
    }
  }

  async changePassword(): Promise<void> {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.savingPassword.set(true);
    try {
      await this.auth.changePassword(this.passwordForm.getRawValue());
      this.notifications.success('Contraseña actualizada. Por seguridad, inicie sesión nuevamente.');
      window.location.href = '/login';
    } catch (error) {
      const problem = error instanceof HttpErrorResponse ? (error.error as ProblemDetails | undefined) : undefined;
      this.notifications.apiError(error, 'No se pudo cambiar la contraseña.');
    } finally {
      this.savingPassword.set(false);
    }
  }
}
