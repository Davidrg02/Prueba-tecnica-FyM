import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';
import { AuthResponse } from '../models/auth.models';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  const authResponse: AuthResponse = {
    accessToken: 'fake-token',
    expiresInSeconds: 900,
    user: {
      id: 'u1',
      userName: 'superadmin',
      email: 'admin@fymtechnology.com',
      fullName: 'Super Administrador',
      roles: ['SuperAdmin'],
      permissions: ['users.read', 'users.create'],
      mustChangePassword: false,
    },
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('starts unauthenticated', () => {
    expect(service.isAuthenticated()).toBeFalse();
    expect(service.accessToken()).toBeNull();
  });

  it('login() stores the access token and user in memory', async () => {
    const promise = service.login({ email: 'admin@fymtechnology.com', password: 'Adm1n#FyM2026*' });

    const req = httpMock.expectOne((r) => r.url.endsWith('/auth/login'));
    expect(req.request.withCredentials).toBeTrue();
    req.flush(authResponse);

    await promise;

    expect(service.isAuthenticated()).toBeTrue();
    expect(service.accessToken()).toBe('fake-token');
    expect(service.currentUser()?.userName).toBe('superadmin');
    expect(service.isSuperAdmin()).toBeTrue();
  });

  it('hasPermission() reflects the permissions granted at login', async () => {
    const promise = service.login({ email: 'a@b.com', password: 'x' });
    httpMock.expectOne((r) => r.url.endsWith('/auth/login')).flush(authResponse);
    await promise;

    expect(service.hasPermission('users.create')).toBeTrue();
    expect(service.hasPermission('roles.delete')).toBeFalse();
  });

  it('clearSession() resets the in-memory state (never touches localStorage)', async () => {
    const promise = service.login({ email: 'a@b.com', password: 'x' });
    httpMock.expectOne((r) => r.url.endsWith('/auth/login')).flush(authResponse);
    await promise;

    service.clearSession();

    expect(service.isAuthenticated()).toBeFalse();
    expect(service.accessToken()).toBeNull();
    expect(localStorage.length).toBe(0);
  });

  it('bootstrap() only runs once even if called twice', async () => {
    const first = service.bootstrap();
    httpMock.expectOne((r) => r.url.endsWith('/auth/refresh')).flush(authResponse);
    await first;

    await service.bootstrap();
    const pendingRefreshCalls = httpMock.match((r) => r.url.endsWith('/auth/refresh'));
    expect(pendingRefreshCalls.length).toBe(0);
  });

  it('bootstrap() clears the session when the silent refresh fails', async () => {
    const promise = service.bootstrap();
    httpMock.expectOne((r) => r.url.endsWith('/auth/refresh')).flush('unauthorized', { status: 401, statusText: 'Unauthorized' });

    await promise;

    expect(service.isAuthenticated()).toBeFalse();
    expect(service.bootstrapped()).toBeTrue();
  });
});
