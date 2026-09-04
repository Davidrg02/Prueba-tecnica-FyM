import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface AppRuntimeConfig {
  apiBaseUrl: string;
}

const DEFAULT_CONFIG: AppRuntimeConfig = { apiBaseUrl: '/api' };

/**
 * Lee `config.json` (servido como archivo estático, no compilado dentro del
 * bundle) para que la misma imagen Docker sirva en cualquier entorno sin
 * reconstruir el frontend: solo cambia `API_BASE_URL` al arrancar el contenedor.
 */
@Injectable({ providedIn: 'root' })
export class ConfigService {
  private readonly http = inject(HttpClient);
  private readonly _config = signal<AppRuntimeConfig>(DEFAULT_CONFIG);

  readonly config = this._config.asReadonly();

  get apiBaseUrl(): string {
    return this._config().apiBaseUrl;
  }

  async load(): Promise<void> {
    try {
      const config = await firstValueFrom(this.http.get<AppRuntimeConfig>('/config.json'));
      this._config.set(config);
    } catch {
      // Sin config.json (por ejemplo, en `ng serve` local) se usa el valor por defecto.
    }
  }
}
