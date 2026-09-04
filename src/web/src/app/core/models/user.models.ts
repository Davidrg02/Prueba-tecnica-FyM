export interface RoleSummary {
  id: number;
  name: string;
  level: number;
}

export interface UserListItem {
  id: string;
  userName: string;
  email: string;
  fullName: string;
  isActive: boolean;
  isSystem: boolean;
  roles: RoleSummary[];
  lastLoginUtc: string | null;
  createdAtUtc: string;
}

export interface UserDetail {
  id: string;
  userName: string;
  email: string;
  firstName: string;
  middleName: string | null;
  lastName: string;
  secondLastName: string | null;
  documentTypeId: number | null;
  documentNumber: string | null;
  phoneNumber: string | null;
  isActive: boolean;
  isSystem: boolean;
  mustChangePassword: boolean;
  roles: RoleSummary[];
  lastLoginUtc: string | null;
  createdAtUtc: string;
}

export interface CreateUserRequest {
  userName: string;
  email: string;
  password: string;
  firstName: string;
  middleName?: string | null;
  lastName: string;
  secondLastName?: string | null;
  documentTypeId?: number | null;
  documentNumber?: string | null;
  phoneNumber?: string | null;
  roleIds: number[];
}

export interface UpdateUserRequest {
  email: string;
  firstName: string;
  middleName?: string | null;
  lastName: string;
  secondLastName?: string | null;
  documentTypeId?: number | null;
  documentNumber?: string | null;
  phoneNumber?: string | null;
}

export interface UpdateUserStatusRequest {
  isActive: boolean;
}

export interface AssignRolesRequest {
  roleIds: number[];
}

export interface ResetPasswordRequest {
  newPassword: string;
}

export interface UpdateOwnProfileRequest {
  firstName: string;
  middleName?: string | null;
  lastName: string;
  secondLastName?: string | null;
  phoneNumber?: string | null;
}

export interface UserQueryParameters {
  page?: number;
  pageSize?: number;
  search?: string;
  sortBy?: string;
  sortDescending?: boolean;
  roleId?: number | null;
  isActive?: boolean | null;
}
