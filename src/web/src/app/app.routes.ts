import { Routes } from '@angular/router';
import { authGuard, guestGuard, permissionGuard } from './core/auth/auth.guard';
import { PermissionCode } from './core/models/auth.models';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register/register.component').then((m) => m.RegisterComponent),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell/shell.component').then((m) => m.ShellComponent),
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () => import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
      },
      {
        path: 'profile',
        loadComponent: () => import('./features/profile/profile.component').then((m) => m.ProfileComponent),
      },
      {
        path: 'users',
        canActivate: [permissionGuard(PermissionCode.UsersRead)],
        loadComponent: () => import('./features/users/user-list/user-list.component').then((m) => m.UserListComponent),
      },
      {
        path: 'users/new',
        canActivate: [permissionGuard(PermissionCode.UsersCreate)],
        loadComponent: () => import('./features/users/user-form/user-form.component').then((m) => m.UserFormComponent),
      },
      {
        path: 'users/:id',
        canActivate: [permissionGuard(PermissionCode.UsersRead)],
        data: { readOnly: true },
        loadComponent: () => import('./features/users/user-form/user-form.component').then((m) => m.UserFormComponent),
      },
      {
        path: 'users/:id/edit',
        canActivate: [permissionGuard(PermissionCode.UsersUpdate)],
        loadComponent: () => import('./features/users/user-form/user-form.component').then((m) => m.UserFormComponent),
      },
      {
        path: 'roles',
        canActivate: [permissionGuard(PermissionCode.RolesRead)],
        loadComponent: () => import('./features/roles/role-list/role-list.component').then((m) => m.RoleListComponent),
      },
      {
        path: 'roles/new',
        canActivate: [permissionGuard(PermissionCode.RolesCreate)],
        loadComponent: () => import('./features/roles/role-detail/role-detail.component').then((m) => m.RoleDetailComponent),
      },
      {
        path: 'roles/:id',
        canActivate: [permissionGuard(PermissionCode.RolesRead)],
        loadComponent: () => import('./features/roles/role-detail/role-detail.component').then((m) => m.RoleDetailComponent),
      },
      {
        path: 'forbidden',
        loadComponent: () => import('./features/errors/forbidden/forbidden.component').then((m) => m.ForbiddenComponent),
      },
      {
        path: '**',
        loadComponent: () => import('./features/errors/not-found/not-found.component').then((m) => m.NotFoundComponent),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
