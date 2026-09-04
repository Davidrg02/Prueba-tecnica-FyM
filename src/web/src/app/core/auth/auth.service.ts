import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ConfigService } from '../config/config.service';
import {
  AuthResponse,
  AuthenticatedUser,
  ChangePasswordRequest,
  LoginRequest,
  RegisterRequest,
  SystemRole,
} from '../models/auth.models';

/**
 * Estado de sesión en memoria. El access token JAMÁS se persiste en
 * localStorage/sessionStorage: vive solo en este signal y se pierde al
 * recargar la página, momento en el que `bootstrap()` lo recupera vía
 * silent refresh usando la cookie HttpOnly que emitió la API.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private static readonly ExplicitLogoutKey = 'fym.explicit-logout';

  private readonly http = inject(HttpClient);
  private readonly config = inject(ConfigService);
  private readonly router = inject(Router, { optional: true });

  private readonly _accessToken = signal<string | null>(null);
  private readonly _user = signal<AuthenticatedUser | null>(null);
  private readonly _bootstrapped = signal(false);
  private expirationTimer: ReturnType<typeof setTimeout> | undefined;

  readonly accessToken = this._accessToken.asReadonly();
  readonly currentUser = this._user.asReadonly();
  readonly bootstrapped = this._bootstrapped.asReadonly();
  readonly isAuthenticated = computed(() => this._user() !== null);
  readonly roles = computed(() => this._user()?.roles ?? []);
  readonly permissions = computed(() => this._user()?.permissions ?? []);
  readonly isSuperAdmin = computed(() => this.roles().includes(SystemRole.SuperAdmin));

  hasPermission(code: string): boolean {
    return this.permissions().includes(code);
  }

  hasExplicitLogout(): boolean {
    return sessionStorage.getItem(AuthService.ExplicitLogoutKey) === 'true';
  }

  private get baseUrl(): string {
    return `${this.config.apiBaseUrl}/v1/auth`;
  }

  async login(request: LoginRequest): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<AuthResponse>(`${this.baseUrl}/login`, request, { withCredentials: true }),
    );
    sessionStorage.removeItem(AuthService.ExplicitLogoutKey);
    this.applyAuthResponse(response);
  }

  async register(request: RegisterRequest): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<AuthResponse>(`${this.baseUrl}/register`, request, { withCredentials: true }),
    );
    sessionStorage.removeItem(AuthService.ExplicitLogoutKey);
    this.applyAuthResponse(response);
  }

  async logout(): Promise<void> {
    try {
      await firstValueFrom(this.http.post(`${this.baseUrl}/logout`, {}, { withCredentials: true }));
    } finally {
      this.clearSession();
      sessionStorage.setItem(AuthService.ExplicitLogoutKey, 'true');
    }
  }

  async changePassword(request: ChangePasswordRequest): Promise<void> {
    await firstValueFrom(this.http.post(`${this.baseUrl}/change-password`, request, { withCredentials: true }));
    this.clearSession();
    sessionStorage.setItem(AuthService.ExplicitLogoutKey, 'true');
  }

  /** Se ejecuta una sola vez al arrancar la app (ver `provideAppInitializer`
   * en app.config.ts): intenta restaurar la sesión con la cookie de refresh
   * antes de que el router resuelva la primera ruta. */
  async bootstrap(): Promise<void> {
    if (this._bootstrapped()) {
      return;
    }

    const initialUrl = this.router?.url || globalThis.location?.pathname || '';
    if (this.hasExplicitLogout() || initialUrl.startsWith('/register')) {
      this.clearSession();
      this._bootstrapped.set(true);
      return;
    }

    try {
      await this.refresh();
    } catch {
      this.clearSession();
    } finally {
      this._bootstrapped.set(true);
    }
  }

  async refresh(): Promise<AuthResponse> {
    const response = await firstValueFrom(
      this.http.post<AuthResponse>(`${this.baseUrl}/refresh`, {}, { withCredentials: true }),
    );
    this.applyAuthResponse(response);
    return response;
  }

  async loadCurrentUser(): Promise<void> {
    const user = await firstValueFrom(
      this.http.get<AuthenticatedUser>(`${this.baseUrl}/me`, { withCredentials: true }),
    );
    this._user.set(user);
  }

  clearSession(): void {
    if (this.expirationTimer !== undefined) {
      clearTimeout(this.expirationTimer);
      this.expirationTimer = undefined;
    }
    this._accessToken.set(null);
    this._user.set(null);
  }

  private applyAuthResponse(response: AuthResponse): void {
    this._accessToken.set(response.accessToken);
    this._user.set(response.user);
    this.scheduleExpiration(response.expiresInSeconds, response.accessToken);
  }

  private scheduleExpiration(expiresInSeconds: number, token: string): void {
    if (this.expirationTimer !== undefined) {
      clearTimeout(this.expirationTimer);
    }

    const delay = Math.max(expiresInSeconds * 1000, 1000);
    this.expirationTimer = setTimeout(() => {
      if (this._accessToken() !== token) {
        return;
      }

      this.clearSession();
      const currentUrl = this.router?.url ?? '/';
      if (!currentUrl.startsWith('/login') && !currentUrl.startsWith('/register')) {
        void this.router?.navigate(['/login'], {
          queryParams: { returnUrl: currentUrl, reason: 'expired' },
        });
      }
    }, delay);
  }
}
