export interface Permission {
  id: number;
  code: string;
  module: string;
  description: string | null;
}

export interface PermissionGroup {
  module: string;
  permissions: Permission[];
}

export interface RoleListItem {
  id: number;
  name: string;
  description: string | null;
  level: number;
  isSystem: boolean;
  userCount: number;
}

export interface RoleDetail {
  id: number;
  name: string;
  description: string | null;
  level: number;
  isSystem: boolean;
  permissions: Permission[];
}

export interface CreateRoleRequest {
  name: string;
  description?: string | null;
  level: number;
}

export interface UpdateRoleRequest {
  name: string;
  description?: string | null;
}

export interface UpdateRolePermissionsRequest {
  permissionIds: number[];
}

export interface DocumentType {
  id: number;
  code: string;
  name: string;
}
