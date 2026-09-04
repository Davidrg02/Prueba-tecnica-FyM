import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RoleApiService } from '../../../core/api/role-api.service';
import { AuthService } from '../../../core/auth/auth.service';
import { PermissionCode } from '../../../core/models/auth.models';
import { RoleListItem } from '../../../core/models/role.models';
import { NotificationService } from '../../../core/notifications/notification.service';
import { ConfirmDialogComponent } from '../../../shared/ui/confirm-dialog/confirm-dialog.component';
import { EmptyStateComponent } from '../../../shared/ui/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../../shared/ui/page-header/page-header.component';

@Component({
  selector: 'fym-role-list',
  imports: [
    RouterLink,
    MatButtonModule,
    MatDialogModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MatTooltipModule,
    EmptyStateComponent,
    PageHeaderComponent,
  ],
  templateUrl: './role-list.component.html',
  styleUrl: './role-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoleListComponent {
  private readonly roleApi = inject(RoleApiService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly dialog = inject(MatDialog);

  readonly displayedColumns = ['name', 'description', 'level', 'userCount', 'actions'];
  readonly loading = signal(true);
  readonly roles = signal<RoleListItem[]>([]);

  readonly canCreate = this.auth.hasPermission(PermissionCode.RolesCreate);
  readonly canUpdate = this.auth.hasPermission(PermissionCode.RolesUpdate);
  readonly canDelete = this.auth.hasPermission(PermissionCode.RolesDelete);

  constructor() {
    void this.reload();
  }

  async reload(): Promise<void> {
    this.loading.set(true);
    try {
      this.roles.set(await this.roleApi.getAll());
    } catch (error) {
      this.roles.set([]);
      this.notifications.apiError(error, 'No se pudieron cargar los roles.');
    } finally {
      this.loading.set(false);
    }
  }

  async deleteRole(role: RoleListItem): Promise<void> {
    if (role.userCount > 0) {
      this.notifications.error('No se puede eliminar un rol con usuarios asignados.');
      return;
    }

    const ref = this.dialog.open(ConfirmDialogComponent, {
      width: '420px',
      data: {
        title: 'Eliminar rol',
        message: `¿Eliminar el rol "${role.name}"? Esta acción no se puede deshacer.`,
        confirmLabel: 'Eliminar',
        danger: true,
      },
    });

    const confirmed = await ref.afterClosed().toPromise();
    if (!confirmed) return;

    try {
      await this.roleApi.delete(role.id);
      this.notifications.success('Rol eliminado.');
      await this.reload();
    } catch (error) {
      this.notifications.apiError(error, 'No se pudo eliminar el rol.');
    }
  }
}
