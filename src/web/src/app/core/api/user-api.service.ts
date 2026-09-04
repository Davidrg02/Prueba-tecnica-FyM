import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ConfigService } from '../config/config.service';
import { PagedResult } from '../models/api.models';
import {
  AssignRolesRequest,
  CreateUserRequest,
  ResetPasswordRequest,
  UpdateOwnProfileRequest,
  UpdateUserRequest,
  UpdateUserStatusRequest,
  UserDetail,
  UserListItem,
  UserQueryParameters,
} from '../models/user.models';

@Injectable({ providedIn: 'root' })
export class UserApiService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ConfigService);

  private get baseUrl(): string {
    return `${this.config.apiBaseUrl}/v1/users`;
  }

  getPaged(params: UserQueryParameters): Promise<PagedResult<UserListItem>> {
    let httpParams = new HttpParams();
    for (const [key, value] of Object.entries(params)) {
      if (value !== null && value !== undefined && value !== '') {
        httpParams = httpParams.set(key, String(value));
      }
    }
    return firstValueFrom(this.http.get<PagedResult<UserListItem>>(this.baseUrl, { params: httpParams }));
  }

  getById(id: string): Promise<UserDetail> {
    return firstValueFrom(this.http.get<UserDetail>(`${this.baseUrl}/${id}`));
  }

  create(request: CreateUserRequest): Promise<UserDetail> {
    return firstValueFrom(this.http.post<UserDetail>(this.baseUrl, request));
  }

  update(id: string, request: UpdateUserRequest): Promise<UserDetail> {
    return firstValueFrom(this.http.put<UserDetail>(`${this.baseUrl}/${id}`, request));
  }

  setStatus(id: string, request: UpdateUserStatusRequest): Promise<void> {
    return firstValueFrom(this.http.patch<void>(`${this.baseUrl}/${id}/status`, request));
  }

  delete(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.baseUrl}/${id}`));
  }

  assignRoles(id: string, request: AssignRolesRequest): Promise<UserDetail> {
    return firstValueFrom(this.http.put<UserDetail>(`${this.baseUrl}/${id}/roles`, request));
  }

  resetPassword(id: string, request: ResetPasswordRequest): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.baseUrl}/${id}/reset-password`, request));
  }

  getOwnProfile(): Promise<UserDetail> {
    return firstValueFrom(this.http.get<UserDetail>(`${this.baseUrl}/me/profile`));
  }

  updateOwnProfile(request: UpdateOwnProfileRequest): Promise<UserDetail> {
    return firstValueFrom(this.http.put<UserDetail>(`${this.baseUrl}/me/profile`, request));
  }
}
