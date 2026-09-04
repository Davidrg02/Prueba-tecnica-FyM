import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ConfigService } from '../config/config.service';
import {
  CreateRoleRequest,
  RoleDetail,
  RoleListItem,
  UpdateRolePermissionsRequest,
  UpdateRoleRequest,
} from '../models/role.models';

@Injectable({ providedIn: 'root' })
export class RoleApiService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ConfigService);

  private get baseUrl(): string {
    return `${this.config.apiBaseUrl}/v1/roles`;
  }

  getAll(): Promise<RoleListItem[]> {
    return firstValueFrom(this.http.get<RoleListItem[]>(this.baseUrl));
  }

  getById(id: number): Promise<RoleDetail> {
    return firstValueFrom(this.http.get<RoleDetail>(`${this.baseUrl}/${id}`));
  }

  create(request: CreateRoleRequest): Promise<RoleDetail> {
    return firstValueFrom(this.http.post<RoleDetail>(this.baseUrl, request));
  }

  update(id: number, request: UpdateRoleRequest): Promise<RoleDetail> {
    return firstValueFrom(this.http.put<RoleDetail>(`${this.baseUrl}/${id}`, request));
  }

  delete(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.baseUrl}/${id}`));
  }

  updatePermissions(id: number, request: UpdateRolePermissionsRequest): Promise<RoleDetail> {
    return firstValueFrom(this.http.put<RoleDetail>(`${this.baseUrl}/${id}/permissions`, request));
  }
}
