import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { debounceTime, startWith } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { UserApiService } from '../../../core/api/user-api.service';
import { RoleApiService } from '../../../core/api/role-api.service';
import { AuthService } from '../../../core/auth/auth.service';
import { PermissionCode, SystemRole } from '../../../core/models/auth.models';
import { RoleListItem } from '../../../core/models/role.models';
import { UserListItem, UserQueryParameters } from '../../../core/models/user.models';
import { NotificationService } from '../../../core/notifications/notification.service';
import { ConfirmDialogComponent } from '../../../shared/ui/confirm-dialog/confirm-dialog.component';
import { EmptyStateComponent } from '../../../shared/ui/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../../shared/ui/page-header/page-header.component';
import { PasswordDialogComponent } from '../../../shared/ui/password-dialog/password-dialog.component';
import { RoleChipsComponent } from '../../../shared/ui/role-chips/role-chips.component';
import { StatusChipComponent } from '../../../shared/ui/status-chip/status-chip.component';

@Component({
  selector: 'fym-user-list',
  imports: [
    DatePipe,
    FormsModule,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatMenuModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSortModule,
    MatTableModule,
    MatTooltipModule,
    EmptyStateComponent,
    PageHeaderComponent,
    RoleChipsComponent,
    StatusChipComponent,
  ],
  templateUrl: './user-list.component.html',
  styleUrl: './user-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserListComponent {
  private readonly userApi = inject(UserApiService);
  private readonly roleApi = inject(RoleApiService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);

  readonly permissionCode = PermissionCode;
  readonly displayedColumns = ['name', 'email', 'roles', 'status', 'lastLogin', 'actions'];

  readonly searchControl = new FormControl('', { nonNullable: true });
  readonly search = toSignal(this.searchControl.valueChanges.pipe(startWith(''), debounceTime(300)), {
    initialValue: '',
  });

  readonly roleFilter = signal<number | null>(null);
  readonly statusFilter = signal<boolean | null>(null);
  readonly page = signal(0);
  readonly pageSize = signal(10);
  readonly sortBy = signal('createdAtUtc');
  readonly sortDescending = signal(true);

  readonly loading = signal(true);
  readonly users = signal<UserListItem[]>([]);
  readonly totalItems = signal(0);
  readonly roles = signal<RoleListItem[]>([]);

  readonly canCreate = this.auth.hasPermission(PermissionCode.UsersCreate);
  readonly canUpdate = this.auth.hasPermission(PermissionCode.UsersUpdate);
  readonly canDelete = this.auth.hasPermission(PermissionCode.UsersDelete);
  readonly canResetPassword = this.auth.hasPermission(PermissionCode.UsersResetPassword);

  private readonly queryParams = computed<UserQueryParameters>(() => ({
    page: this.page() + 1,
    pageSize: this.pageSize(),
    search: this.search() || undefined,
    roleId: this.roleFilter(),
    isActive: this.statusFilter(),
    sortBy: this.sortBy(),
    sortDescending: this.sortDescending(),
  }));

  constructor() {
    void this.loadRoles();
    effect(() => {
      const params = this.queryParams();
      void this.reload(params);
    });
  }

  onFilterChange(): void {
    this.page.set(0);
  }

  onPageChange(event: PageEvent): void {
    this.page.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
  }

  onSortChange(sort: Sort): void {
    if (!sort.direction) {
      this.sortBy.set('createdAtUtc');
      this.sortDescending.set(true);
    } else {
      this.sortBy.set(sort.active);
      this.sortDescending.set(sort.direction === 'desc');
    }
  }

  async reload(params = this.queryParams()): Promise<void> {
    this.loading.set(true);
    try {
      const result = await this.userApi.getPaged(params);
      this.users.set(result.items);
      this.totalItems.set(result.totalItems);
    } catch {
      this.users.set([]);
      this.totalItems.set(0);
    } finally {
      this.loading.set(false);
    }
  }

  private async loadRoles(): Promise<void> {
    try {
      this.roles.set(await this.roleApi.getAll());
    } catch {
      this.roles.set([]);
    }
  }

  isSelf(user: UserListItem): boolean {
    return user.id === this.auth.currentUser()?.id;
  }

  /** Regla de negocio replicada en el cliente para no ofrecer acciones que
   * el backend rechazará: solo un super administrador puede operar sobre otro. */
  canManage(user: UserListItem): boolean {
    if (this.auth.isSuperAdmin()) {
      return true;
    }
    return !user.roles.some((role) => role.name === SystemRole.SuperAdmin);
  }

  async toggleStatus(user: UserListItem): Promise<void> {
    const activate = !user.isActive;
    const confirmed = await this.confirm({
      title: activate ? 'Activar usuario' : 'Desactivar usuario',
      message: activate
        ? `¿Activar a ${user.fullName}? Podrá iniciar sesión nuevamente.`
        : `¿Desactivar a ${user.fullName}? No podrá iniciar sesión hasta que se reactive.`,
      confirmLabel: activate ? 'Activar' : 'Desactivar',
      danger: !activate,
    });
    if (!confirmed) {
      return;
    }

    await this.userApi.setStatus(user.id, { isActive: activate });
    this.notifications.success(activate ? 'Usuario activado.' : 'Usuario desactivado.');
    await this.reload();
  }

  async deleteUser(user: UserListItem): Promise<void> {
    const confirmed = await this.confirm({
      title: 'Eliminar usuario',
      message: `Esta acción eliminará a ${user.fullName} del sistema. ¿Desea continuar?`,
      confirmLabel: 'Eliminar',
      danger: true,
    });
    if (!confirmed) {
      return;
    }

    await this.userApi.delete(user.id);
    this.notifications.success('Usuario eliminado.');
    await this.reload();
  }

  async resetPassword(user: UserListItem): Promise<void> {
    const ref = this.dialog.open(PasswordDialogComponent, { width: '440px' });
    const newPassword = await ref.afterClosed().toPromise();
    if (!newPassword) {
      return;
    }

    try {
      await this.userApi.resetPassword(user.id, { newPassword });
      this.notifications.success('Contraseña restablecida. El usuario deberá cambiarla en su próximo ingreso.');
    } catch (error) {
      this.notifications.apiError(error, 'No se pudo restablecer la contraseña.');
    }
  }

  viewUser(user: UserListItem): void {
    void this.router.navigate(['/users', user.id]);
  }

  private async confirm(data: { title: string; message: string; confirmLabel: string; danger?: boolean }): Promise<boolean> {
    const ref = this.dialog.open(ConfirmDialogComponent, { data, width: '420px' });
    const result = await ref.afterClosed().toPromise();
    return Boolean(result);
  }
}
