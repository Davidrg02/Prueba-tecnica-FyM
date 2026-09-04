import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { PermissionCode } from '../../core/models/auth.models';

interface NavItem {
  label: string;
  icon: string;
  path: string;
  permission?: string;
}

const NAV_ITEMS: NavItem[] = [
  { label: 'Panel', icon: 'dashboard', path: '/' },
  { label: 'Usuarios', icon: 'group', path: '/users', permission: PermissionCode.UsersRead },
  { label: 'Roles', icon: 'admin_panel_settings', path: '/roles', permission: PermissionCode.RolesRead },
  { label: 'Mi perfil', icon: 'person', path: '/profile' },
];

@Component({
  selector: 'fym-shell',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatDividerModule,
  ],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShellComponent {
  private readonly auth = inject(AuthService);
  private readonly breakpointObserver = inject(BreakpointObserver);

  private readonly isHandset = toSignal(
    this.breakpointObserver.observe(Breakpoints.Handset).pipe(map((result) => result.matches)),
    { initialValue: false },
  );

  readonly sidenavMode = computed(() => (this.isHandset() ? 'over' : 'side'));
  readonly sidenavOpened = signal(true);

  readonly currentUser = this.auth.currentUser;
  readonly navItems = computed(() => NAV_ITEMS.filter((item) => !item.permission || this.auth.hasPermission(item.permission)));

  readonly initials = computed(() => {
    const name = this.currentUser()?.fullName ?? this.currentUser()?.userName ?? '';
    return name
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase())
      .join('');
  });

  readonly primaryRoleLabel = computed(() => this.auth.roles()[0] ?? '');

  toggleSidenav(): void {
    this.sidenavOpened.update((value) => !value);
  }

  async logout(): Promise<void> {
    await this.auth.logout();
    window.location.href = '/login';
  }
}
