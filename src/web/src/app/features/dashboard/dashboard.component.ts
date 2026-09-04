import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { UserApiService } from '../../core/api/user-api.service';
import { RoleApiService } from '../../core/api/role-api.service';
import { AuthService } from '../../core/auth/auth.service';
import { PermissionCode } from '../../core/models/auth.models';
import { RoleListItem } from '../../core/models/role.models';
import { UserListItem } from '../../core/models/user.models';
import { RoleChipsComponent } from '../../shared/ui/role-chips/role-chips.component';

@Component({
  selector: 'fym-dashboard',
  imports: [RouterLink, MatCardModule, MatIconModule, MatProgressSpinnerModule, RoleChipsComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent {
  private readonly userApi = inject(UserApiService);
  private readonly roleApi = inject(RoleApiService);
  private readonly auth = inject(AuthService);

  readonly currentUser = this.auth.currentUser;
  readonly canSeeUserStats = this.auth.hasPermission(PermissionCode.UsersRead);

  readonly loading = signal(this.canSeeUserStats);
  readonly totalUsers = signal(0);
  readonly activeUsers = signal(0);
  readonly inactiveUsers = signal(0);
  readonly roles = signal<RoleListItem[]>([]);
  readonly recentUsers = signal<UserListItem[]>([]);

  constructor() {
    if (this.canSeeUserStats) {
      void this.loadStats();
    }
  }

  private async loadStats(): Promise<void> {
    try {
      const [total, active, inactive, roles, recent] = await Promise.all([
        this.userApi.getPaged({ page: 1, pageSize: 1 }),
        this.userApi.getPaged({ page: 1, pageSize: 1, isActive: true }),
        this.userApi.getPaged({ page: 1, pageSize: 1, isActive: false }),
        this.roleApi.getAll(),
        this.userApi.getPaged({ page: 1, pageSize: 5, sortBy: 'createdAtUtc', sortDescending: true }),
      ]);

      this.totalUsers.set(total.totalItems);
      this.activeUsers.set(active.totalItems);
      this.inactiveUsers.set(inactive.totalItems);
      this.roles.set(roles);
      this.recentUsers.set(recent.items);
    } finally {
      this.loading.set(false);
    }
  }
}
