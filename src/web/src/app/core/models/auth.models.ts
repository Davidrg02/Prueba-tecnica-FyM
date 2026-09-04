export interface AuthenticatedUser {
  id: string;
  userName: string;
  email: string;
  fullName: string;
  roles: string[];
  permissions: string[];
  mustChangePassword: boolean;
}

export interface AuthResponse {
  accessToken: string;
  expiresInSeconds: number;
  user: AuthenticatedUser;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  userName: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  documentTypeId?: number | null;
  documentNumber?: string | null;
  phoneNumber?: string | null;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

/** Códigos de permiso — deben coincidir exactamente con FyM.Users.Domain.Enums.PermissionCode. */
export const PermissionCode = {
  UsersRead: 'users.read',
  UsersCreate: 'users.create',
  UsersUpdate: 'users.update',
  UsersDelete: 'users.delete',
  UsersAssignRoles: 'users.assign-roles',
  UsersResetPassword: 'users.reset-password',
  RolesRead: 'roles.read',
  RolesCreate: 'roles.create',
  RolesUpdate: 'roles.update',
  RolesDelete: 'roles.delete',
  RolesManagePermissions: 'roles.manage-permissions',
  AuditRead: 'audit.read',
  ProfileReadOwn: 'profile.read-own',
  ProfileUpdateOwn: 'profile.update-own',
} as const;

export type PermissionCodeValue = (typeof PermissionCode)[keyof typeof PermissionCode];

export const SystemRole = {
  SuperAdmin: 'SuperAdmin',
  Admin: 'Admin',
  User: 'User',
} as const;
