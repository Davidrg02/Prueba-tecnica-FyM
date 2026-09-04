import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { CatalogApiService } from '../../../core/api/catalog-api.service';
import { RoleApiService } from '../../../core/api/role-api.service';
import { UserApiService } from '../../../core/api/user-api.service';
import { AuthService } from '../../../core/auth/auth.service';
import { PermissionCode, SystemRole } from '../../../core/models/auth.models';
import { ProblemDetails } from '../../../core/models/api.models';
import { DocumentType, RoleListItem } from '../../../core/models/role.models';
import { UserDetail } from '../../../core/models/user.models';
import { NotificationService } from '../../../core/notifications/notification.service';
import { ConfirmDialogComponent } from '../../../shared/ui/confirm-dialog/confirm-dialog.component';
import { PageHeaderComponent } from '../../../shared/ui/page-header/page-header.component';

const PASSWORD_PATTERN = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s]).{8,}$/;

@Component({
  selector: 'fym-user-form',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    PageHeaderComponent,
  ],
  templateUrl: './user-form.component.html',
  styleUrl: './user-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly userApi = inject(UserApiService);
  private readonly roleApi = inject(RoleApiService);
  private readonly catalogApi = inject(CatalogApiService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly userId = this.route.snapshot.paramMap.get('id');
  readonly isEditMode = this.userId !== null;
  readonly readOnly = this.route.snapshot.data['readOnly'] === true;

  readonly loading = signal(true);
  readonly loadError = signal(false);
  readonly saving = signal(false);
  readonly hidePassword = signal(true);
  readonly documentTypes = signal<DocumentType[]>([]);
  readonly roles = signal<RoleListItem[]>([]);
  readonly user = signal<UserDetail | null>(null);

  readonly canUpdate = this.auth.hasPermission(PermissionCode.UsersUpdate);
  readonly canAssignRoles = this.auth.hasPermission(PermissionCode.UsersAssignRoles);
  readonly canDelete = this.auth.hasPermission(PermissionCode.UsersDelete);
  readonly canResetPassword = this.auth.hasPermission(PermissionCode.UsersResetPassword);

  readonly canManageTarget = computed(() => {
    const target = this.user();
    if (!target) {
      return true;
    }
    if (this.auth.isSuperAdmin()) {
      return true;
    }
    return !target.roles.some((role) => role.name === SystemRole.SuperAdmin);
  });

  readonly isSelf = computed(() => this.user()?.id === this.auth.currentUser()?.id);

  readonly accountForm = this.fb.nonNullable.group({
    userName: ['', [Validators.required, Validators.maxLength(50)]],
    email: ['', [Validators.required, Validators.email]],
    password: [''],
  });

  readonly profileForm = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    middleName: [''],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    secondLastName: [''],
    documentTypeId: this.fb.control<number | null>(null),
    documentNumber: [''],
    phoneNumber: [''],
  });

  readonly roleIds = signal<number[]>([]);

  constructor() {
    if (this.isEditMode) {
      this.accountForm.controls.userName.disable();
    } else {
      this.accountForm.controls.password.addValidators([Validators.required, Validators.pattern(PASSWORD_PATTERN)]);
    }

    if (this.readOnly) {
      this.accountForm.disable();
      this.profileForm.disable();
    }

    void this.loadInitialData();
  }

  private async loadInitialData(): Promise<void> {
    try {
      const [documentTypes, roles] = await Promise.all([this.catalogApi.getDocumentTypes(), this.roleApi.getAll()]);
      this.documentTypes.set(documentTypes);
      this.roles.set(roles);

      if (this.isEditMode && this.userId) {
        const user = await this.userApi.getById(this.userId);
        this.user.set(user);
        this.accountForm.patchValue({ userName: user.userName, email: user.email });
        this.profileForm.patchValue({
          firstName: user.firstName,
          middleName: user.middleName ?? '',
          lastName: user.lastName,
          secondLastName: user.secondLastName ?? '',
          documentTypeId: user.documentTypeId,
          documentNumber: user.documentNumber ?? '',
          phoneNumber: user.phoneNumber ?? '',
        });
        this.roleIds.set(user.roles.map((role) => role.id));
      }
    } catch {
      this.loadError.set(true);
      this.notifications.error('No se pudo cargar la información del usuario.');
    } finally {
      this.loading.set(false);
    }
  }

  toggleRole(roleId: number): void {
    if (this.readOnly) {
      return;
    }

    const current = this.roleIds();
    this.roleIds.set(current.includes(roleId) ? current.filter((id) => id !== roleId) : [...current, roleId]);
  }

  async submit(event?: Event): Promise<void> {
    event?.preventDefault();
    event?.stopPropagation();

    if (this.accountForm.invalid || this.profileForm.invalid || this.roleIds().length === 0) {
      this.accountForm.markAllAsTouched();
      this.profileForm.markAllAsTouched();
      if (this.roleIds().length === 0) {
        this.notifications.error('Debe seleccionar al menos un rol.');
      }
      return;
    }

    this.saving.set(true);
    try {
      if (this.isEditMode && this.userId) {
        await this.userApi.update(this.userId, {
          email: this.accountForm.getRawValue().email,
          ...this.profileForm.getRawValue(),
        });
        if (this.canAssignRoles && !this.readOnly) {
          await this.userApi.assignRoles(this.userId, { roleIds: this.roleIds() });
        }
        this.notifications.success('Usuario actualizado correctamente.');
      } else {
        await this.userApi.create({
          ...this.accountForm.getRawValue(),
          ...this.profileForm.getRawValue(),
          roleIds: this.roleIds(),
        });
        this.notifications.success('Usuario creado correctamente.');
      }
      await this.router.navigate(['/users']);
    } catch (error) {
      const problem = error instanceof HttpErrorResponse ? (error.error as ProblemDetails | undefined) : undefined;
      this.notifications.apiError(error, 'No se pudo guardar el usuario.');
    } finally {
      this.saving.set(false);
    }
  }

  async toggleStatus(): Promise<void> {
    const target = this.user();
    if (!target) return;

    const activate = !target.isActive;
    const confirmed = await this.confirm(
      activate ? 'Activar usuario' : 'Desactivar usuario',
      activate ? '¿Activar esta cuenta?' : '¿Desactivar esta cuenta? El usuario no podrá iniciar sesión.',
      activate ? 'Activar' : 'Desactivar',
      !activate,
    );
    if (!confirmed) return;

    await this.userApi.setStatus(target.id, { isActive: activate });
    this.notifications.success(activate ? 'Usuario activado.' : 'Usuario desactivado.');
    this.user.set({ ...target, isActive: activate });
  }

  async resetPassword(): Promise<void> {
    const target = this.user();
    if (!target) return;

    const newPassword = window.prompt('Ingrese la nueva contraseña temporal (mín. 8 caracteres, mayúscula, minúscula, dígito y símbolo):');
    if (!newPassword) return;

    await this.userApi.resetPassword(target.id, { newPassword });
    this.notifications.success('Contraseña restablecida. El usuario deberá cambiarla en su próximo ingreso.');
  }

  async deleteUser(): Promise<void> {
    const target = this.user();
    if (!target) return;

    const confirmed = await this.confirm(
      'Eliminar usuario',
      `Esta acción eliminará a ${target.firstName} ${target.lastName} del sistema. ¿Desea continuar?`,
      'Eliminar',
      true,
    );
    if (!confirmed) return;

    await this.userApi.delete(target.id);
    this.notifications.success('Usuario eliminado.');
    await this.router.navigate(['/users']);
  }

  private async confirm(title: string, message: string, confirmLabel: string, danger: boolean): Promise<boolean> {
    const ref = this.dialog.open(ConfirmDialogComponent, { data: { title, message, confirmLabel, danger }, width: '420px' });
    const result = await ref.afterClosed().toPromise();
    return Boolean(result);
  }
}
