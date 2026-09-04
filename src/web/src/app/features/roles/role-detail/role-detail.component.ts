import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CatalogApiService } from '../../../core/api/catalog-api.service';
import { RoleApiService } from '../../../core/api/role-api.service';
import { AuthService } from '../../../core/auth/auth.service';
import { PermissionCode } from '../../../core/models/auth.models';
import { PermissionGroup } from '../../../core/models/role.models';
import { NotificationService } from '../../../core/notifications/notification.service';
import { PageHeaderComponent } from '../../../shared/ui/page-header/page-header.component';

@Component({
  selector: 'fym-role-detail',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    PageHeaderComponent,
  ],
  templateUrl: './role-detail.component.html',
  styleUrl: './role-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoleDetailComponent {
  private readonly fb = inject(FormBuilder);
  private readonly roleApi = inject(RoleApiService);
  private readonly catalogApi = inject(CatalogApiService);
  private readonly notifications = inject(NotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);

  private readonly roleId = this.route.snapshot.paramMap.get('id');
  readonly isEditMode = this.roleId !== null;
  readonly isSystemRole = signal(false);
  readonly canUpdate = this.auth.hasPermission(PermissionCode.RolesUpdate);
  readonly canManagePermissions = this.auth.hasPermission(PermissionCode.RolesManagePermissions);
  readonly readOnly = this.isEditMode && !this.canUpdate && !this.canManagePermissions;

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly permissionGroups = signal<PermissionGroup[]>([]);
  readonly selectedPermissionIds = signal<Set<number>>(new Set());

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(50)]],
    description: [''],
    level: this.fb.nonNullable.control<number>(10, [Validators.required, Validators.min(1), Validators.max(99)]),
  });

  constructor() {
    if (this.readOnly) {
      this.form.disable();
    }
    void this.loadInitialData();
  }

  private async loadInitialData(): Promise<void> {
    try {
      const groups = await this.catalogApi.getPermissionsGrouped();
      this.permissionGroups.set(groups);

      if (this.isEditMode && this.roleId) {
        const role = await this.roleApi.getById(Number(this.roleId));
        this.isSystemRole.set(role.isSystem);
        this.form.patchValue({ name: role.name, description: role.description ?? '', level: role.level });
        if (role.isSystem || !this.canUpdate) {
          this.form.controls.name.disable();
          this.form.controls.description.disable();
        }
        if (role.isSystem) {
          this.form.controls.level.disable();
        }
        this.selectedPermissionIds.set(new Set(role.permissions.map((p) => p.id)));
      }
    } catch (error) {
      this.notifications.apiError(error, 'No se pudo cargar la información del rol.');
    } finally {
      this.loading.set(false);
    }
  }

  isModuleFullySelected(group: PermissionGroup): boolean {
    return group.permissions.every((p) => this.selectedPermissionIds().has(p.id));
  }

  isModulePartiallySelected(group: PermissionGroup): boolean {
    const selected = group.permissions.filter((p) => this.selectedPermissionIds().has(p.id)).length;
    return selected > 0 && selected < group.permissions.length;
  }

  togglePermission(id: number): void {
    if (this.readOnly || !this.canManagePermissions) {
      return;
    }

    const next = new Set(this.selectedPermissionIds());
    next.has(id) ? next.delete(id) : next.add(id);
    this.selectedPermissionIds.set(next);
  }

  toggleModule(group: PermissionGroup): void {
    if (this.readOnly || !this.canManagePermissions) {
      return;
    }

    const next = new Set(this.selectedPermissionIds());
    const shouldSelectAll = !this.isModuleFullySelected(group);
    for (const permission of group.permissions) {
      shouldSelectAll ? next.add(permission.id) : next.delete(permission.id);
    }
    this.selectedPermissionIds.set(next);
  }

  async submit(event?: Event): Promise<void> {
    event?.preventDefault();
    event?.stopPropagation();

    if (this.readOnly) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    try {
      const value = this.form.getRawValue();

      if (this.isEditMode && this.roleId) {
        const id = Number(this.roleId);
        if (!this.isSystemRole() && this.canUpdate) {
          await this.roleApi.update(id, { name: value.name, description: value.description || null });
        }
        if (this.canManagePermissions) {
          await this.roleApi.updatePermissions(id, { permissionIds: [...this.selectedPermissionIds()] });
        }
        this.notifications.success('Rol actualizado correctamente.');
      } else {
        const created = await this.roleApi.create({
          name: value.name,
          description: value.description || null,
          level: value.level,
        });
        await this.roleApi.updatePermissions(created.id, { permissionIds: [...this.selectedPermissionIds()] });
        this.notifications.success('Rol creado correctamente.');
      }

      await this.router.navigate(['/roles']);
    } catch (error) {
      this.notifications.apiError(error, 'No se pudo guardar el rol.');
    } finally {
      this.saving.set(false);
    }
  }
}
