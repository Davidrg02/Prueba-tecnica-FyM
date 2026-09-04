import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ConfigService } from '../config/config.service';
import { DocumentType, PermissionGroup } from '../models/role.models';

@Injectable({ providedIn: 'root' })
export class CatalogApiService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ConfigService);

  getDocumentTypes(): Promise<DocumentType[]> {
    return firstValueFrom(this.http.get<DocumentType[]>(`${this.config.apiBaseUrl}/v1/catalogs/document-types`));
  }

  getPermissionsGrouped(): Promise<PermissionGroup[]> {
    return firstValueFrom(this.http.get<PermissionGroup[]>(`${this.config.apiBaseUrl}/v1/permissions`));
  }
}
